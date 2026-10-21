using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace EarnedResurrection;

public sealed class QuestNode_Resurrection : QuestNode_Root_AncientComplex
{
    private enum Lead { Outpost, Mechanoids, Turrets, Manhunters, Ambush, Complex, Terminal, Drone, Final }
    protected override LayoutDef LayoutDef => DefDatabase<LayoutDef>.GetNamed("ER_AncientComplex");
    private static IEnumerable<Faction> EnemyFactions => Find.FactionManager.AllFactions.Where(f =>
        !f.IsPlayer && !f.temporary && !f.defeated && !f.Hidden && f.HostileTo(Faction.OfPlayer) &&
        f.def.humanlikeFaction && f.def.pawnGroupMakers != null &&
        f.def.pawnGroupMakers.Any(g => g.kindDef == PawnGroupKindDefOf.Settlement));
    private static int LeadCount => Math.Max(1, Math.Min(5, ResurrectionMod.Settings?.leadCount ?? 3));

    private static List<Lead> AvailableLeads()
    {
        var result = new List<Lead> { Lead.Outpost, Lead.Turrets, Lead.Manhunters, Lead.Ambush };
        if (Faction.OfMechanoids != null) result.Add(Lead.Mechanoids);
        if (ModsConfig.IdeologyActive)
        {
            result.Add(Lead.Complex);
            result.Add(Lead.Terminal);
            if (Faction.OfMechanoids != null) result.Add(Lead.Drone);
        }
        return result;
    }

    protected override bool TestRunInt(Slate slate) => Find.Storyteller.difficulty.allowViolentQuests &&
        EnemyFactions.Any() && AvailableLeads().Count >= LeadCount && TileFinder.TryFindNewSiteTile(out _, 2, 10);

    private static List<Lead> SelectLeads()
    {
        var pool = AvailableLeads();
        var selected = new List<Lead>();
        while (selected.Count < LeadCount)
        {
            // Ideology encounters are the primary path; Core encounters extend
            // longer chains or are the entire fallback when the DLC is absent.
            var primary = pool.Where(k => k == Lead.Complex || k == Lead.Terminal || k == Lead.Drone).ToList();
            var candidates = primary.Count > 0 ? primary : pool;
            Lead next = candidates.RandomElementByWeight(k => k == Lead.Manhunters ? 0.5f :
                k == Lead.Drone ? 1f : 2f);
            selected.Add(next);
            pool.Remove(next);
        }
        selected.Add(Lead.Final);
        return selected;
    }

    protected override void RunInt()
    {
        Quest quest = QuestGen.quest;
        string pawnName = QuestGen.slate.Get<string>("pawnName") ?? "ER_UnknownColonist".Translate().Resolve();
        QuestGen.slate.Set("resolvedQuestName", "ER_QuestTitle".Translate(pawnName).Resolve());
        QuestGen.slate.Set("resolvedQuestDescription", "ER_QuestDescription".Translate(pawnName).Resolve());
        if (!EnemyFactions.TryRandomElement(out Faction enemy))
            throw new InvalidOperationException("No hostile outpost faction.");
        float points = Math.Max(300f, QuestGen.slate.Get("points", 0f)) * ResurrectionTuning.ThreatMultiplier;
        points = Math.Max(points, enemy.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Settlement));
        int interval = (ResurrectionMod.Settings?.betweenStagesDays ?? 1) * ResurrectionTuning.TicksPerDay;
        string enable = QuestGen.slate.Get<string>("inSignal");
        var cleanup = new QuestPart_ResurrectionCleanup();
        quest.AddPart(cleanup);
        List<Lead> plan = SelectLeads();
        for (int index = 0; index < plan.Count; index++)
        {
            Lead kind = plan[index];
            bool final = kind == Lead.Final;
            string completed = QuestGen.GenerateNewSignal("ER_EncounterCompleted");
            float budget = points * (final ? ResurrectionTuning.FinalThreatMultiplier : 1f);
            var terminals = new List<Thing>();
            Site site = MakeEncounter(kind, budget, enemy, terminals);
            site.customLabel = ("ER_Name_" + kind).Translate().Resolve();
            string tag = QuestGen.GenerateNewTargetQuestTag("ER_location");
            QuestUtility.AddQuestTag(site, tag);
            quest.SpawnWorldObject(site, inSignal: enable);
            cleanup.sites.Add(site);
            string description = ("ER_Description_" + kind).Translate().Resolve();
            quest.DescriptionPart(description, enable, completed);
            quest.Letter(LetterDefOf.NeutralEvent, inSignal: enable, label: site.customLabel,
                text: description, lookTargets: Gen.YieldSingle(site));
            AddFailure(quest, enable, completed, tag + ".MapRemoved");
            AddFailure(quest, enable, completed, tag + ".Destroyed");
            string expired = QuestGen.GenerateNewSignal("ER_Expired");
            var timeout = new QuestPart_WorldObjectTimeout
            {
                inSignalEnable = enable, inSignalDisable = completed, worldObject = site,
                delayTicks = ResurrectionTuning.StageDeadlineTicks, destroyOnCleanup = true
            };
            timeout.outSignalsCompleted.Add(expired);
            quest.AddPart(timeout);
            quest.End(QuestEndOutcome.Fail, inSignal: expired, sendStandardLetter: true);

            if (final)
            {
                var stash = site.parts.First(p => p.def.defName == "ItemStash");
                var cache = (Building_SerumCache)stash.things.First(t => t.def.defName == "ER_SerumCache");
                cache.ownerQuest = quest;
                cache.openedSignal = completed;
                cleanup.cache = cache;
                Thing serum = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("MechSerumResurrector"));
                serum.stackCount = 1;
                if (!cache.TryAcceptThing(serum)) throw new InvalidOperationException("Serum cache rejected contents.");
                var reward = new QuestPart_Choice.Choice();
                reward.rewards.Add(new Reward_Items { items = new List<Thing> { serum } });
                quest.RewardChoice().choices.Add(reward);
                string cacheTag = QuestGen.GenerateNewTargetQuestTag("ER_cache");
                QuestUtility.AddQuestTag(cache, cacheTag);
                AddFailure(quest, enable, completed, cacheTag + ".Destroyed");
                // Opening the physical cache ejects the single stored serum before
                // vanilla Building_Casket emits this signal. No drop-pod reward.
                quest.End(QuestEndOutcome.Success, inSignal: completed, sendStandardLetter: true);
            }
            else
            {
                var objective = new QuestPart_ArchiveObjective
                {
                    inSignalEnable = enable, site = site, terminals = terminals,
                    clearanceSignal = tag + ".AllEnemiesDefeated"
                };
                objective.outSignalsCompleted.Add(completed);
                foreach (Thing terminal in terminals)
                {
                    string terminalTag = QuestGen.GenerateNewTargetQuestTag("ER_databank");
                    QuestUtility.AddQuestTag(terminal, terminalTag);
                    objective.hackedSignals.Add(terminalTag + ".Hacked");
                    string failed = QuestGen.GenerateNewSignal("ER_DataLost");
                    quest.AddPart(new QuestPart_Filter_Hacked { inSignal = terminalTag + ".Destroyed", outSignalElse = failed });
                    AddFailure(quest, enable, completed, failed);
                }
                quest.AddPart(objective);
                string next = QuestGen.GenerateNewSignal("ER_NextLead");
                string progress = interval == 0 ? "ER_LeadRecoveredImmediate".Translate().Resolve() :
                    "ER_LeadRecovered".Translate(interval.ToStringTicksToPeriod()).Resolve();
                // A direct modal message, not a transient notification or letter.
                quest.Dialog(progress, "ER_ProgressTitle".Translate().Resolve(), inSignal: completed);
                quest.DescriptionPart(progress, completed, next);
                var delay = new QuestPart_Delay { inSignalEnable = completed, delayTicks = interval,
                    expiryInfoPart = "ER_NextLeadTimer".Translate().Resolve() };
                delay.outSignalsCompleted.Add(next);
                quest.AddPart(delay);
                enable = next;
            }
        }
        Log.Message("[EarnedResurrection] Encounter plan: " + string.Join(", ", plan));
    }

    private static void AddFailure(Quest quest, string enable, string completed, string signal) =>
        quest.SignalPassActivable(inSignalEnable: enable, inSignal: signal, inSignalDisable: completed,
            action: () => quest.End(QuestEndOutcome.Fail, sendStandardLetter: true));

    private Site MakeEncounter(Lead kind, float points, Faction enemy, List<Thing> terminals)
    {
        if (!TileFinder.TryFindNewSiteTile(out PlanetTile tile, 2, 10))
            throw new InvalidOperationException("No expedition tile.");
        bool final = kind == Lead.Final;
        bool complex = kind == Lead.Complex || (final && ModsConfig.IdeologyActive);
        Faction faction = complex ? Faction.OfAncientsHostile : enemy;
        var parts = new List<SitePartDefWithParams>();
        if (complex)
        {
            LayoutStructureSketch sketch = GenerateStructureSketch(points, generateTerminals: !final);
            if (!final) terminals.AddRange(sketch.thingsToSpawn);
            parts.Add(new SitePartDefWithParams(SitePartDefOf.AncientComplex, new SitePartParams
            {
                ancientLayoutStructureSketch = sketch, points = points,
                threatPoints = ThreatPointsOverPointsCurve.Evaluate(points),
                ancientComplexRewardMaker = ThingSetMakerDefOf.MapGen_AncientComplexRoomLoot_Default
            }));
            // Guarantee a combat component instead of relying only on random room threats.
            AddThreat(parts, Faction.OfMechanoids != null ? "SleepingMechanoids" : "AmbushEdge", points, tile, enemy);
        }
        else
        {
            string threat = kind == Lead.Mechanoids || kind == Lead.Drone ? "SleepingMechanoids" :
                kind == Lead.Turrets ? "Turrets" : kind == Lead.Manhunters ? "Manhunters" :
                kind == Lead.Ambush ? "AmbushEdge" : "Outpost";
            if (threat == "Turrets") faction = Faction.OfAncientsHostile;
            AddThreat(parts, threat, points, tile, faction);
        }

        bool stashNeeded = final || !complex;
        var restore = QuestGen.slate.GetRestoreInfo("itemStashSingleThing");
        if (stashNeeded)
        {
            string defName = final ? "ER_SerumCache" : kind == Lead.Terminal ? "AncientTerminal_Worshipful" :
                kind == Lead.Drone ? "Spacedrone" : "ComponentIndustrial";
            QuestGen.slate.Set("itemStashSingleThing", DefDatabase<ThingDef>.GetNamed(defName));
            // Stash first: vanilla outpost/turret gen steps defend its rect of interest.
            parts.Insert(0, new SitePartDefWithParams(DefDatabase<SitePartDef>.GetNamed("ItemStash"), new SitePartParams()));
        }
        Site site;
        try { site = QuestGen_Sites.GenerateSite(parts, tile, faction); }
        finally { QuestGen.slate.Restore(restore); }
        if (kind == Lead.Terminal || kind == Lead.Drone)
            terminals.AddRange(site.parts.First(p => p.def.defName == "ItemStash").things.Where(t => t.TryGetComp<CompHackable>() != null));
        foreach (Thing terminal in terminals) terminal.TryGetComp<CompHackable>().defence = 1800f;
        return site;
    }

    private static void AddThreat(List<SitePartDefWithParams> parts, string name, float points, PlanetTile tile, Faction faction)
    {
        SitePartDef def = DefDatabase<SitePartDef>.GetNamed(name);
        SitePartParams parms = def.Worker.GenerateDefaultParams(points, tile, faction);
        parms.lootMarketValue = 0f;
        parts.Add(new SitePartDefWithParams(def, parms));
    }

    public override LayoutStructureSketch QuestSetupComplex(Quest quest, float points) => GenerateStructureSketch(points);
}
