using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace EarnedResurrection;

// Saved event-driven hacking + security objective, independent of relic state.
public sealed class QuestPart_ArchiveObjective : QuestPartActivable
{
    public Site site;
    public List<Thing> terminals = new List<Thing>();
    public List<string> hackedSignals = new List<string>();
    public string clearanceSignal;

    protected override void ProcessQuestSignal(Signal signal)
    {
        if (signal.tag != clearanceSignal && !hackedSignals.Contains(signal.tag)) return;
        foreach (Thing terminal in terminals)
            if (terminal.DestroyedOrNull() || !terminal.TryGetComp<CompHackable>().IsHacked) return;
        if (site?.Map == null || GenHostility.AnyHostileActiveThreatToPlayer(site.Map, true, true)) return;
        Complete();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref site, "site");
        Scribe_Collections.Look(ref terminals, "terminals", LookMode.Reference);
        Scribe_Collections.Look(ref hackedSignals, "hackedSignals", LookMode.Value);
        Scribe_Values.Look(ref clearanceSignal, "clearanceSignal");
    }
}
