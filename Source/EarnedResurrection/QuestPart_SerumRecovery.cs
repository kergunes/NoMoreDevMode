using RimWorld;
using RimWorld.Planet;
using Verse;

namespace EarnedResurrection;

// Event-driven final check: past clearance signals cannot authorize victory
// while newly revealed/dormant threats remain. No per-pawn or per-tick polling.
public sealed class QuestPart_SerumRecovery : QuestPart
{
    public Site site;
    // Legacy saved name: now also used for records recovered by securing an outpost.
    public string terminalsHackedSignal;
    public string clearanceSignal;
    public string outSignal;
    private bool terminalsHacked;
    private bool completed;

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        if (completed || (signal.tag != terminalsHackedSignal && signal.tag != clearanceSignal))
            return;
        if (signal.tag == terminalsHackedSignal)
            terminalsHacked = true;
        if (!terminalsHacked || site?.Map == null ||
            GenHostility.AnyHostileActiveThreatToPlayer(site.Map, countDormantPawnsAsHostile: true, canBeFogged: true))
            return;
        completed = true;
        Find.SignalManager.SendSignal(new Signal(outSignal));
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref site, "site");
        Scribe_Values.Look(ref terminalsHackedSignal, "terminalsHackedSignal");
        Scribe_Values.Look(ref clearanceSignal, "clearanceSignal");
        Scribe_Values.Look(ref outSignal, "outSignal");
        Scribe_Values.Look(ref terminalsHacked, "terminalsHacked");
        Scribe_Values.Look(ref completed, "completed");
    }
}
