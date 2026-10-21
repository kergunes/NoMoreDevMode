using HarmonyLib;
using RimWorld;
using Verse;

namespace EarnedResurrection;

[HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
public static class ColonistDeathPatch
{
    // Snapshot before vanilla removes the pawn from the map/caravan and trackers.
    public static void Prefix(Pawn __instance, out bool __state)
    {
        __state = Current.ProgramState == ProgramState.Playing && IsEligible(__instance);
    }

    public static void Postfix(Pawn __instance, bool __state)
    {
        if (__state && __instance.Dead)
            Current.Game?.GetComponent<ResurrectionState>()?.NotifyEligibleDeath(__instance);
    }

    public static bool IsEligible(Pawn pawn)
    {
        if (pawn == null || pawn.Dead || pawn.Destroyed || !pawn.RaceProps.Humanlike ||
            pawn.Faction != Faction.OfPlayer || !pawn.IsFreeColonist || pawn.IsPrisoner ||
            pawn.IsSlave || pawn.HostFaction != null || pawn.IsQuestLodger() ||
            pawn.HasExtraHomeFaction() || pawn.records == null)
            return false;

        // This persisted vanilla record excludes time as prisoner/slave/guest.
        // Cumulative actual membership is intentional, rather than time since spawn.
        if (pawn.records.GetValue(RecordDefOf.TimeAsColonistOrColonyAnimal) <
            ResurrectionTuning.MinimumMembershipTicks)
            return false;

        return PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists_NoLodgers.Contains(pawn);
    }
}
