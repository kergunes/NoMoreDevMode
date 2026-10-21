using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace EarnedResurrection;

// One native subquest at a time, generated outside QuestGen's non-reentrant
// generation pass. The parent remains the owner of the serum and stage delays.
public sealed class QuestPart_DlcLead : QuestPartActivable
{
    public float points;
    public string fallbackSignal;
    private Quest child;
    private bool started;
    private int deadline = -1;

    public override string DescriptionPart => State == QuestPartState.Enabled && child != null ?
        "ER_DlcLeadActive".Translate(child.name).Resolve() : null;
    public override string ExpiryInfoPart => State == QuestPartState.Enabled && child != null ?
        "ER_DlcLeadDeadline".Translate(Math.Max(0, deadline - Find.TickManager.TicksGame)
            .ToStringTicksToPeriod()).Resolve() : null;
    public override IEnumerable<GlobalTargetInfo> QuestLookTargets =>
        child?.QuestLookTargets ?? Enumerable.Empty<GlobalTargetInfo>();

    public override void QuestPartTick()
    {
        if (!started)
        {
            started = true;
            try { StartLead(); }
            catch (Exception error)
            {
                Log.Error("[EarnedResurrection] DLC lead generation failed: " + error);
                quest.End(QuestEndOutcome.Fail);
            }
            return;
        }
        if (child == null || Find.TickManager.TicksGame % 250 != 0) return;
        if (child.State == QuestState.EndedSuccess)
            Complete();
        else if (child.Historical || Find.TickManager.TicksGame >= deadline)
            quest.End(QuestEndOutcome.Fail);
    }

    private void StartLead()
    {
        QuestPart_DlcPool pool = quest.GetFirstPartOfType<QuestPart_DlcPool>();
        var candidates = DlcLeadCatalog.Entries.Where(e => pool.allowed.Contains(e.name) &&
            !pool.used.Contains(e.name)).ToList();
        while (candidates.Count > 0)
        {
            var entry = candidates.RandomElementByWeight(e => e.weight);
            candidates.Remove(entry);
            QuestScriptDef def = DefDatabase<QuestScriptDef>.GetNamedSilentFail(
                entry.name == "ProblemCauser" ? "ER_HumanProblemCauser" : entry.name);
            Map home = Find.AnyPlayerHomeMap;
            if (def == null || home == null || !Surface(home)) continue;
            var slate = new Slate();
            slate.Set("points", points);
            // Hospitality's native higher difficulties can select excluded mech
            // clusters. Use peaceful hospitality plus our explicit human raid.
            slate.Set("challengeRating", entry.name == "Hospitality_Joiners" ? 1 : 2);
            slate.Set("discovered", true);
            slate.Set("discoveryMethod", "ER_DlcDiscovery".Translate().Resolve());
            if (entry.name == "AncientComplex_Mission")
            {
                if (!ModsConfig.RoyaltyActive) continue;
                Precept_Relic relic = Faction.OfPlayer.ideos?.PrimaryIdeo?
                    .GetAllPreceptsOfType<Precept_Relic>().FirstOrDefault();
                if (relic == null) continue;
                // The native generator requires this for text/sketch setup.
                // Its informational reward is removed before registration; no
                // real relic hunt is used as a parent or advanced by this quest.
                slate.Set("relic", relic);
            }
            if (!def.CanRun(slate, home)) continue;

            pool.used.Add(entry.name);
            child = QuestGen.Generate(def, slate);
            if (child == null)
            {
                Log.Error("[EarnedResurrection] Could not generate DLC lead " + entry.name);
                quest.End(QuestEndOutcome.Fail);
                return;
            }
            child.parent = quest;
            PrepareChild(entry, home);
            deadline = Find.TickManager.TicksGame + ResurrectionTuning.StageDeadlineTicks;
            Find.QuestManager.Add(child);
            QuestUtility.SendLetterQuestAvailable(child);
            Log.Message("[EarnedResurrection] DLC lead: " + entry.name);
            return;
        }
        // No eligible unused DLC root: use this slot's unique Core encounter.
        Disable();
        Find.SignalManager.SendSignal(new Signal(fallbackSignal));
    }

    private void PrepareChild(DlcLeadCatalog.Entry entry, Map home)
    {
        if (!child.PartsListForReading.OfType<QuestPart_QuestEnd>().Any(e => e.outcome == QuestEndOutcome.Success))
            throw new InvalidOperationException("DLC generator did not produce a success objective: " + entry.name);
        // Information is the lead reward. Do not let generated reward choices
        // deliver an extra serum, relic information, honor or recruitment.
        foreach (QuestPart_Choice choice in child.PartsListForReading.OfType<QuestPart_Choice>().ToList())
        {
            foreach (QuestPart rewardPart in choice.choices.SelectMany(c => c.questParts).Distinct().ToList())
                if (child.PartsListForReading.Contains(rewardPart)) child.RemovePart(rewardPart);
            child.RemovePart(choice);
        }
        foreach (QuestPart_QuestEnd ending in child.PartsListForReading.OfType<QuestPart_QuestEnd>())
        { ending.sendLetter = false; ending.playSound = false; }

        string titleKey = "ER_DlcName_" + entry.name;
        child.name = titleKey.Translate().Resolve();
        child.description = "ER_DlcLeadIntroduction".Translate(quest.name).Resolve() + "\n\n" + child.description;
        var sites = child.PartsListForReading.OfType<QuestPart_SpawnWorldObject>()
            .Select(p => p.worldObject).OfType<Site>().Distinct().ToList();
        foreach (Site site in sites)
        {
            if (!Surface(site)) throw new InvalidOperationException("A DLC lead selected a non-surface site.");
            site.customLabel = child.name;
            QuestUtility.AddQuestTag(site, ShipmentRewardPolicy.SiteTag);
            // Fail unfinished abandonment even when the native opportunity
            // normally treats a removed map as an unknown outcome.
            string tag = "Quest" + child.id + ".ER_leadSite";
            QuestUtility.AddQuestTag(site, tag);
            child.AddPart(new QuestPart_QuestEnd { inSignal = tag + ".MapRemoved", outcome = QuestEndOutcome.Fail });
            child.AddPart(new QuestPart_QuestEnd { inSignal = tag + ".Destroyed", outcome = QuestEndOutcome.Fail });
        }

        if (entry.archiveSite)
        {
            if (sites.Count != 1) throw new InvalidOperationException("Expected one DLC archive site.");
            AddArchiveObjective(sites[0]);
            child.description += "\n\n" + "ER_DlcArchiveObjective".Translate();
        }
        else if (entry.name == "Hospitality_Joiners")
        {
            MapParent target = child.PartsListForReading.OfType<QuestPart_PawnsArrive>()
                .Select(p => p.mapParent).FirstOrDefault(p => p?.Map != null && p.Map.IsPlayerHome) ??
                child.QuestLookTargets.Select(t => t.WorldObject).OfType<MapParent>()
                .FirstOrDefault(p => p.Map != null && p.Map.IsPlayerHome) ?? home.Parent;
            Faction attackers = QuestNode_Resurrection.EnemyFactions.RandomElement();
            child.AddPart(new QuestPart_RandomRaid
            {
                inSignal = child.InitiateSignal,
                mapParent = target, faction = attackers, pointsRange = new FloatRange(points, points),
                arrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn, raidStrategy = RaidStrategyDefOf.ImmediateAttack
            });
            child.description += "\n\n" + "ER_DlcDefendObjective".Translate();
            AddCombatGate();
        }
        if (entry.name == "AncientComplex_Mission")
        {
            Precept_Relic relic = Faction.OfPlayer.ideos.PrimaryIdeo.GetAllPreceptsOfType<Precept_Relic>().First();
            child.description = child.description.Replace(relic.Label, "ER_SiteLabel".Translate().Resolve());
        }
        child.description += "\n\n" + "ER_SpecialRewardsDamaged".Translate().Resolve();
    }

    private void AddArchiveObjective(Site site)
    {
        // Native arrival/claim/no-active-threat success is replaced by a real
        // guarded interaction. The native terrain/layout/occupants are retained.
        foreach (QuestPart ending in child.PartsListForReading.Where(p =>
            p is QuestPart_QuestEnd end && end.outcome == QuestEndOutcome.Success).ToList())
            child.RemovePart(ending);

        var cache = (Building_SerumCache)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("ER_ArchiveCache"));
        cache.ownerQuest = child;
        cache.openedSignal = "Quest" + child.id + ".ER_recordsRecovered";
        cache.TryAcceptThing(ThingMaker.MakeThing(ThingDefOf.ComponentIndustrial));
        var stash = new SitePart(site, DefDatabase<SitePartDef>.GetNamed("ItemStash"), new SitePartParams());
        stash.things = new ThingOwner<Thing>(stash, false);
        stash.things.TryAdd(cache);
        site.AddPart(stash);
        string guards = Faction.OfMechanoids != null ? "SleepingMechanoids" : "Turrets";
        SitePartDef guardDef = DefDatabase<SitePartDef>.GetNamed(guards);
        site.AddPart(new SitePart(site, guardDef, guardDef.Worker.GenerateDefaultParams(points, site.Tile, site.Faction)));
        string tag = "Quest" + child.id + ".ER_records";
        QuestUtility.AddQuestTag(cache, tag);
        child.AddPart(new QuestPart_QuestEnd { inSignal = cache.openedSignal, outcome = QuestEndOutcome.Success });
        child.AddPart(new QuestPart_QuestEnd { inSignal = tag + ".Destroyed", outcome = QuestEndOutcome.Fail });
        var cleanup = new QuestPart_ResurrectionCleanup { cache = cache };
        cleanup.sites.Add(site);
        child.AddPart(cleanup);
    }

    private void AddCombatGate()
    {
        var gate = new QuestPart_DlcCombatGate { inSignalEnable = child.InitiateSignal };
        gate.maps.AddRange(Find.Maps.Where(m => m.IsPlayerHome && Surface(m)));
        foreach (QuestPart_QuestEnd ending in child.PartsListForReading.OfType<QuestPart_QuestEnd>()
                     .Where(e => e.outcome == QuestEndOutcome.Success).ToList())
        {
            gate.successSignals.Add(ending.inSignal);
            child.RemovePart(ending);
        }
        child.AddPart(gate);
    }

    private static bool Surface(MapParent parent)
    {
        return parent.Tile.LayerDef == PlanetLayerDefOf.Surface;
    }
    private static bool Surface(Map map) => Surface(map.Parent);

    public override void Cleanup()
    {
        if (child != null && !child.Historical) child.End(QuestEndOutcome.Unknown, false, false);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref points, "points");
        Scribe_Values.Look(ref fallbackSignal, "fallbackSignal");
        Scribe_References.Look(ref child, "child");
        Scribe_Values.Look(ref started, "started");
        Scribe_Values.Look(ref deadline, "deadline", -1);
    }
}
