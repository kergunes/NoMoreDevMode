using RimWorld;
using Verse;

namespace EarnedResurrection;

public sealed class Building_SerumCache : Building_Casket
{
    public Quest ownerQuest;

    public override bool CanOpen => base.CanOpen && Spawned && ownerQuest != null &&
        ownerQuest.State == QuestState.Ongoing &&
        !GenHostility.AnyHostileActiveThreatToPlayer(Map, true, true);

    public override void Open()
    {
        if (!CanOpen) return;
        EjectContents();
        // Do not report success if vanilla could not place the contents nearby.
        if (!HasAnyContents && !openedSignal.NullOrEmpty())
            Find.SignalManager.SendSignal(new Signal(openedSignal, this.Named("SUBJECT")));
    }

    public override void EjectContents()
    {
        if (CanOpen) base.EjectContents();
    }

    public void DestroyReward() => innerContainer.ClearAndDestroyContents();

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        // Shooting/deconstructing the cache must not bypass its guarded opening.
        DestroyReward();
        base.Destroy(mode);
    }

    public override string GetInspectString()
    {
        string text = base.GetInspectString();
        if (HasAnyContents && !CanOpen)
            text += "\n" + "ER_CacheLocked".Translate();
        return text;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref ownerQuest, "ownerQuest");
    }
}
