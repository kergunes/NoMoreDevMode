using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace EarnedResurrection;

public sealed class QuestNode_Resurrection : QuestNode_Root_AncientComplex
{
    protected override LayoutDef LayoutDef => DefDatabase<LayoutDef>.GetNamed("ER_AncientComplex");

    protected override bool TestRunInt(Slate slate) => ModsConfig.IdeologyActive &&
        Find.Storyteller.difficulty.allowViolentQuests && Faction.OfMechanoids != null &&
        TileFinder.TryFindNewSiteTile(out _, 2, 10, false, AllowedLandmarks);

    protected override void RunInt()
    {
        Quest quest = QuestGen.quest;
        Slate slate = QuestGen.slate;
        string name = slate.Get<string>("pawnName") ?? "ER_UnknownColonist".Translate().Resolve();
        slate.Set("resolvedQuestName", "ER_QuestTitle".Translate(name).Resolve());
        slate.Set("resolvedQuestDescription", "ER_QuestDescription".Translate(name).Resolve());
        float points = Math.Max(300f, slate.Get("points", 0f)) * ResurrectionTuning.ThreatMultiplier;
        string enable = slate.Get<string>("inSignal");
        var cleanup = new QuestPart_ResurrectionCleanup();
        // Cleanup must run before vanilla destroys unspawned world objects.
        quest.AddPart(cleanup);

        for (int stage = 1; stage <= 4; stage++)
        {
            bool final = stage == 4;
            string completed = QuestGen.GenerateNewSignal("ER_Stage" + stage + "Completed");
            string hacked = final ? QuestGen.GenerateNewSignal("ER_FinalTerminalsHacked") : completed;
            float stagePoints = points * (final ? ResurrectionTuning.FinalThreatMultiplier : 1f);
            LayoutStructureSketch sketch = QuestSetupComplex(quest, stagePoints);
            if (!TileFinder.TryFindNewSiteTile(out PlanetTile tile, 2, 10, false, AllowedLandmarks))
                throw new InvalidOperationException("No suitable ancient complex tile.");

            float complexThreat = ThreatPointsOverPointsCurve.Evaluate(stagePoints);
            var parts = new List<SitePartDefWithParams>
            {
                new SitePartDefWithParams(SitePartDefOf.AncientComplex, new SitePartParams
                {
                    ancientLayoutStructureSketch = sketch,
                    ancientComplexRewardMaker = ThingSetMakerDefOf.MapGen_AncientComplexRoomLoot_Default,
                    threatPoints = complexThreat,
                    points = stagePoints
                })
            };
            if (final)
            {
                SitePartDef guards = DefDatabase<SitePartDef>.GetNamed("SleepingMechanoids");
                parts.Add(new SitePartDefWithParams(guards,
                    guards.Worker.GenerateDefaultParams(stagePoints, tile, Faction.OfMechanoids)));
            }

            Site site = QuestGen_Sites.GenerateSite(parts, tile, Faction.OfAncientsHostile);
            site.customLabel = "ER_SiteLabel".Translate(stage).Resolve();
            string siteTag = QuestGen.GenerateNewTargetQuestTag("ER_site" + stage);
            QuestUtility.AddQuestTag(site, siteTag);
            quest.SpawnWorldObject(site, inSignal: enable);
            cleanup.sites.Add(site);

            var pass = new QuestPart_PassAllActivable
            {
                inSignalEnable = enable,
                expiryInfoPartKey = "ER_TerminalsHacked"
            };
            pass.outSignalsCompleted.Add(hacked);
            foreach (Thing terminal in sketch.thingsToSpawn)
            {
                string terminalTag = QuestGen.GenerateNewTargetQuestTag("ER_terminal");
                QuestUtility.AddQuestTag(terminal, terminalTag);
                pass.inSignals.Add(terminalTag + ".Hacked");
                terminal.TryGetComp<CompHackable>().defence = Rand.Bool ? 300 : 1800;
                string failed = QuestGen.GenerateNewSignal("ER_TerminalDestroyed");
                quest.AddPart(new QuestPart_Filter_Hacked
                {
                    inSignal = terminalTag + ".Destroyed", outSignalElse = failed
                });
                quest.SignalPassActivable(inSignalEnable: enable, inSignal: failed,
                    inSignalDisable: completed,
                    action: () => quest.End(QuestEndOutcome.Fail, sendStandardLetter: true));
            }
            quest.AddPart(pass);
            quest.DescriptionPart((final ? "ER_FinalDescription" : "ER_StageDescription").Translate(stage).Resolve(), enable, completed);
            quest.Letter(LetterDefOf.NeutralEvent, inSignal: enable,
                label: "ER_StageLetterTitle".Translate(stage).Resolve(),
                text: (final ? "ER_FinalDescription" : "ER_StageDescription").Translate(stage).Resolve(),
                lookTargets: Gen.YieldSingle(site));

            string timeoutSignal = QuestGen.GenerateNewSignal("ER_StageExpired");
            var timeout = new QuestPart_WorldObjectTimeout
            {
                inSignalEnable = enable, inSignalDisable = completed,
                delayTicks = ResurrectionTuning.StageDeadlineTicks, worldObject = site,
                destroyOnCleanup = true
            };
            timeout.outSignalsCompleted.Add(timeoutSignal);
            quest.AddPart(timeout);
            quest.End(QuestEndOutcome.Fail, inSignal: timeoutSignal, sendStandardLetter: true);
            quest.SignalPassActivable(inSignalEnable: enable, inSignal: siteTag + ".MapRemoved",
                inSignalDisable: completed,
                action: () => quest.End(QuestEndOutcome.Fail, sendStandardLetter: true));
            quest.SignalPassActivable(inSignalEnable: enable, inSignal: siteTag + ".Destroyed",
                inSignalDisable: completed,
                action: () => quest.End(QuestEndOutcome.Fail, sendStandardLetter: true));

            if (final)
            {
                quest.AddPart(new QuestPart_SerumRecovery
                {
                    site = site, terminalsHackedSignal = hacked,
                    clearanceSignal = siteTag + ".AllEnemiesDefeated", outSignal = completed
                });
                Thing serum = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("MechSerumResurrector"));
                serum.stackCount = 1;
                var choice = new QuestPart_Choice.Choice();
                choice.rewards.Add(new Reward_Items { items = new List<Thing> { serum } });
                quest.RewardChoice().choices.Add(choice);
                // The same single instance is displayed and delivered. Never a reward roll.
                var delivery = quest.DropPods(site, Gen.YieldSingle(serum), inSignal: completed,
                    allowFogged: false, dropAllInSamePod: true);
                string delivered = QuestGen.GenerateNewSignal("ER_SerumDelivered");
                delivery.outSignalResult = delivered;
                quest.End(QuestEndOutcome.Success, inSignal: delivered, sendStandardLetter: true);
            }
            else
            {
                string next = QuestGen.GenerateNewSignal("ER_NextLead");
                var delay = new QuestPart_Delay
                {
                    inSignalEnable = completed,
                    delayTicks = ResurrectionTuning.BetweenStagesTicks
                };
                delay.outSignalsCompleted.Add(next);
                quest.AddPart(delay);
                enable = next;
            }
        }
    }

    public override LayoutStructureSketch QuestSetupComplex(Quest quest, float points)
    {
        LayoutStructureSketch sketch = GenerateStructureSketch(points);
        sketch.thingDiscoveredMessage = "ER_TerminalDiscovered".Translate().Resolve();
        return sketch;
    }
}
