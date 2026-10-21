using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace EarnedResurrection;

// The existing independent Ideology encounters, now selectable alongside the
// other DLC roots. They do not create or advance an ideology's relic hunt.
public sealed class QuestNode_ArchiveLead : QuestNode
{
    public string kind;

    protected override bool TestRunInt(Slate slate) => ModsConfig.IdeologyActive &&
        (kind != "Drone" || Faction.OfMechanoids != null) &&
        QuestNode_Resurrection.EnemyFactions.Any() && TileFinder.TryFindNewSiteTile(out _, 2, 10);

    protected override void RunInt()
    {
        var lead = (QuestNode_Resurrection.Lead)Enum.Parse(typeof(QuestNode_Resurrection.Lead), kind);
        Quest quest = QuestGen.quest;
        string enable = QuestGen.slate.Get<string>("inSignal");
        string completed = QuestGen.GenerateNewSignal("ER_ArchiveRecovered");
        var terminals = new List<Thing>();
        Site site = new QuestNode_Resurrection().MakeEncounter(lead, QuestGen.slate.Get("points", 300f),
            QuestNode_Resurrection.EnemyFactions.RandomElement(), terminals);
        site.customLabel = ("ER_Name_" + kind).Translate().Resolve();
        QuestGen.slate.Set("resolvedQuestName", site.customLabel);
        QuestGen.slate.Set("resolvedQuestDescription", ("ER_Description_" + kind).Translate().Resolve());
        string tag = QuestGen.GenerateNewTargetQuestTag("ER_archive");
        QuestUtility.AddQuestTag(site, tag);
        quest.SpawnWorldObject(site);
        var cleanup = new QuestPart_ResurrectionCleanup();
        cleanup.sites.Add(site);
        quest.AddPart(cleanup);
        var objective = new QuestPart_ArchiveObjective
        {
            inSignalEnable = enable, site = site, terminals = terminals,
            clearanceSignal = tag + ".AllEnemiesDefeated"
        };
        objective.outSignalsCompleted.Add(completed);
        foreach (Thing terminal in terminals)
        {
            string terminalTag = QuestGen.GenerateNewTargetQuestTag("ER_terminal");
            QuestUtility.AddQuestTag(terminal, terminalTag);
            objective.hackedSignals.Add(terminalTag + ".Hacked");
            string failed = QuestGen.GenerateNewSignal("ER_DataLost");
            quest.AddPart(new QuestPart_Filter_Hacked { inSignal = terminalTag + ".Destroyed", outSignalElse = failed });
            quest.End(QuestEndOutcome.Fail, inSignal: failed);
        }
        quest.AddPart(objective);
        quest.End(QuestEndOutcome.Fail, inSignal: tag + ".MapRemoved");
        quest.End(QuestEndOutcome.Fail, inSignal: tag + ".Destroyed");
        quest.End(QuestEndOutcome.Success, inSignal: completed);
    }
}
