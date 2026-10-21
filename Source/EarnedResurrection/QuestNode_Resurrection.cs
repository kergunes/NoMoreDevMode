using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace EarnedResurrection;

public sealed class QuestNode_Resurrection : QuestNode
{
    private static IEnumerable<Faction> EnemyFactions => Find.FactionManager.AllFactions.Where(f =>
        !f.IsPlayer && !f.temporary && !f.defeated && !f.Hidden && f.HostileTo(Faction.OfPlayer) &&
        f.def.humanlikeFaction && f.def.pawnGroupMakers != null &&
        f.def.pawnGroupMakers.Any(g => g.kindDef == PawnGroupKindDefOf.Settlement));

    protected override bool TestRunInt(Slate slate) =>
        Find.Storyteller.difficulty.allowViolentQuests && EnemyFactions.Any() &&
        TileFinder.TryFindNewSiteTile(out _, 2, 10);

    protected override void RunInt()
    {
        Quest quest = QuestGen.quest;
        Slate slate = QuestGen.slate;
        string name = slate.Get<string>("pawnName") ?? "ER_UnknownColonist".Translate().Resolve();
        slate.Set("resolvedQuestName", "ER_QuestTitle".Translate(name).Resolve());
        slate.Set("resolvedQuestDescription", "ER_QuestDescription".Translate(name).Resolve());
        float points = Math.Max(300f, slate.Get("points", 0f)) * ResurrectionTuning.ThreatMultiplier;
        if (!EnemyFactions.TryRandomElement(out Faction enemy))
            throw new InvalidOperationException("No hostile faction capable of defending an outpost.");
        // Use the same faction throughout, including its vanilla minimum budget,
        // so the final's multiplier cannot be flattened by a different faction.
        points = Math.Max(points, enemy.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Settlement));
        string enable = slate.Get<string>("inSignal");
        // Freeze this quest's intervals at generation; vanilla delay parts save them.
        int betweenStagesTicks = (ResurrectionMod.Settings?.betweenStagesDays ??
            ResurrectionTuning.DefaultBetweenStagesDays) * ResurrectionTuning.TicksPerDay;
        var cleanup = new QuestPart_ResurrectionCleanup();
        // Cleanup must run before vanilla destroys unspawned world objects.
        quest.AddPart(cleanup);

        for (int stage = 1; stage <= 4; stage++)
        {
            bool final = stage == 4;
            string completed = QuestGen.GenerateNewSignal("ER_Stage" + stage + "Completed");
            float stagePoints = points * (final ? ResurrectionTuning.FinalThreatMultiplier : 1f);
            if (!TileFinder.TryFindNewSiteTile(out PlanetTile tile, 2, 10))
                throw new InvalidOperationException("No suitable outpost tile.");

            SitePartDef outpost = DefDatabase<SitePartDef>.GetNamed("Outpost");
            SitePartParams outpostParams = outpost.Worker.GenerateDefaultParams(stagePoints, tile, enemy);
            // No randomly rolled site reward; the only quest serum is the final pod.
            outpostParams.lootMarketValue = 0f;
            var parts = new List<SitePartDefWithParams>
            {
                new SitePartDefWithParams(outpost, outpostParams)
            };

            Site site = QuestGen_Sites.GenerateSite(parts, tile, enemy);
            site.customLabel = "ER_SiteLabel".Translate(stage).Resolve();
            string siteTag = QuestGen.GenerateNewTargetQuestTag("ER_site" + stage);
            QuestUtility.AddQuestTag(site, siteTag);
            quest.SpawnWorldObject(site, inSignal: enable);
            cleanup.sites.Add(site);

            string secured = siteTag + ".AllEnemiesDefeated";
            if (!final)
                quest.SignalPassActivable(inSignalEnable: enable, inSignal: secured, outSignalCompleted: completed);
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
                    // Retain the old field name for existing saved quest parts.
                    // New quests recover the records when the outpost is secured.
                    site = site, terminalsHackedSignal = secured,
                    clearanceSignal = secured, outSignal = completed
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
                string recoveredText = betweenStagesTicks == 0
                    ? "ER_LeadRecoveredImmediate".Translate().Resolve()
                    : "ER_LeadRecovered".Translate(betweenStagesTicks.ToStringTicksToPeriod()).Resolve();
                quest.Message(recoveredText, MessageTypeDefOf.PositiveEvent, inSignal: completed);
                quest.DescriptionPart(recoveredText, completed, next);
                var delay = new QuestPart_Delay
                {
                    inSignalEnable = completed,
                    delayTicks = betweenStagesTicks,
                    expiryInfoPart = "ER_NextLeadTimer".Translate().Resolve()
                };
                delay.outSignalsCompleted.Add(next);
                quest.AddPart(delay);
                enable = next;
            }
        }
    }

}
