using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace EarnedResurrection;

public sealed class QuestPart_ResurrectionCleanup : QuestPart
{
    public List<Site> sites = new List<Site>();
    public Building_SerumCache cache;

    public override void Cleanup()
    {
        if (quest.State != QuestState.EndedSuccess) cache?.DestroyReward();
        Current.Game?.GetComponent<ResurrectionState>()?.NotifyQuestEnded(quest);
        foreach (Site site in sites)
            QuestPart_DestroyWorldObject.TryRemove(site);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        // Sites are deeply owned by vanilla SpawnWorldObject until spawned.
        Scribe_Collections.Look(ref sites, "sites", LookMode.Reference);
        Scribe_References.Look(ref cache, "cache");
    }
}
