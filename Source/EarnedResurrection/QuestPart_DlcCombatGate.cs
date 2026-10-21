using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace EarnedResurrection;

// Some native crash quests finish on a timer or inspection while their troops
// can still be fighting. Preserve that prerequisite and also require clearance.
public sealed class QuestPart_DlcCombatGate : QuestPartActivable
{
    public List<string> successSignals = new List<string>();
    public List<Map> maps = new List<Map>();
    private bool nativeSuccess;

    protected override void ProcessQuestSignal(Signal signal)
    {
        if (successSignals.Contains(signal.tag)) nativeSuccess = true;
    }

    public override void QuestPartTick()
    {
        if (nativeSuccess && Find.TickManager.TicksGame % 250 == 0 &&
            maps.All(m => m != null && !TransporterUtility.IncomingTransporterPreventingMapRemoval(m) &&
                !GenHostility.AnyHostileActiveThreatToPlayer(m, true, true)))
        {
            Complete();
            quest.End(QuestEndOutcome.Success, false, false);
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref successSignals, "successSignals", LookMode.Value);
        Scribe_Collections.Look(ref maps, "maps", LookMode.Reference);
        Scribe_Values.Look(ref nativeSuccess, "nativeSuccess");
    }
}
