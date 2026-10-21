using LudeonTK;
using Verse;

namespace EarnedResurrection;

public static class ResurrectionDebugActions
{
    [DebugAction("Earned Resurrection", "Print resurrection state")]
    public static void PrintState()
    {
        var state = Current.Game.GetComponent<ResurrectionState>();
        Log.Message($"[EarnedResurrection] Active={state.ChainActive}, quest={state.ActiveQuest?.id}, pending={state.PendingTick}, cooldownEnd={state.NextEligibleTick}.");
    }

    [DebugAction("Earned Resurrection", "Clear resurrection cooldown")]
    public static void ClearCooldown() => Current.Game.GetComponent<ResurrectionState>().DebugClearCooldown();
}
