using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace EarnedResurrection;

[HarmonyPatch(typeof(ThingMaker), nameof(ThingMaker.MakeThing))]
public static class ShipmentThingCapturePatch
{
    public static void Postfix(Thing __result) => ShipmentRewardPolicy.CurrentGeneration?.things.Add(__result);
}

[HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), new[] { typeof(PawnGenerationRequest) })]
public static class ShipmentPawnCapturePatch
{
    public static void Postfix(Pawn __result) => ShipmentRewardPolicy.CurrentGeneration?.things.Add(__result);
}

[HarmonyPatch(typeof(Site), nameof(Site.PostMapGenerate))]
public static class ShipmentSiteLootPatch
{
    public static void Postfix(Site __instance) => ShipmentRewardPolicy.SanitizeMap(__instance.Map);
}

[HarmonyPatch(typeof(CompLootSpawn), nameof(CompLootSpawn.PostSpawnSetup))]
public static class ShipmentCrateLootPatch
{
    public static void Postfix(CompLootSpawn __instance)
    {
        if (__instance.parent is Building_Casket casket) ShipmentRewardPolicy.SanitizeContainer(casket);
    }
}

[HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.TryDropEquipment))]
public static class ShipmentWeaponDropPatch
{
    public static bool Prefix(Pawn_EquipmentTracker __instance, ThingWithComps eq,
        ref ThingWithComps resultingEq, ref bool __result)
    {
        if (!ShipmentRewardPolicy.Marked(eq) || !ShipmentRewardPolicy.IsUnique(eq)) return true;
        __instance.Remove(eq);
        eq.Destroy(DestroyMode.Vanish);
        resultingEq = null;
        __result = true; // The drop was handled; vanilla callers accept a null result.
        return false;
    }
}

[HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.TryTransferEquipmentToContainer))]
public static class ShipmentWeaponTransferPatch
{
    public static bool Prefix(Pawn_EquipmentTracker __instance, ThingWithComps eq, ref bool __result)
    {
        if (!ShipmentRewardPolicy.Marked(eq) || !ShipmentRewardPolicy.IsUnique(eq)) return true;
        __instance.Remove(eq);
        eq.Destroy(DestroyMode.Vanish);
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(Building_GravEngine), nameof(Building_GravEngine.Inspect))]
public static class ShipmentEngineInspectPatch
{
    public static bool Prefix(Building_GravEngine __instance)
    {
        if (!ShipmentRewardPolicy.Marked(__instance)) return true;
        if (!__instance.questTags.Contains(ShipmentRewardPolicy.InspectedTag))
        {
            __instance.questTags.Add(ShipmentRewardPolicy.InspectedTag);
            // Keep the original native quest signal. Do not unlock research,
            // grant ownership, or start the vanilla gravship progression chain.
            QuestUtility.SendQuestTargetSignals(__instance.questTags, "Inspected", __instance.Named("SUBJECT"));
            Messages.Message("ER_EngineRecordsRecovered".Translate(), __instance, MessageTypeDefOf.NeutralEvent);
        }
        return false;
    }
}

[HarmonyPatch(typeof(Building_GravEngine), nameof(Building_GravEngine.ClaimableBy))]
public static class ShipmentEngineClaimPatch
{
    public static void Postfix(Building_GravEngine __instance, ref AcceptanceReport __result)
    {
        if (ShipmentRewardPolicy.Marked(__instance)) __result = "ER_EngineRuined".Translate().Resolve();
    }
}

[HarmonyPatch(typeof(Building_GravEngine), nameof(Building_GravEngine.CanLaunch))]
public static class ShipmentEngineLaunchPatch
{
    public static void Postfix(Building_GravEngine __instance, ref AcceptanceReport __result)
    {
        if (ShipmentRewardPolicy.Marked(__instance)) __result = "ER_EngineRuined".Translate().Resolve();
    }
}

[HarmonyPatch(typeof(Building_GravEngine), nameof(Building_GravEngine.GetFloatMenuOptions))]
public static class ShipmentEngineMenuPatch
{
    public static void Postfix(Building_GravEngine __instance, Pawn selPawn, ref IEnumerable<FloatMenuOption> __result)
    {
        if (ShipmentRewardPolicy.Marked(__instance))
            __result = __result.Where(option => option.Label !=
                "CommandInspectGravEngine".Translate(__instance).Resolve()).Concat(Options(__instance, selPawn));
    }

    private static IEnumerable<FloatMenuOption> Options(Building_GravEngine engine, Pawn pawn)
    {
        // Remains available even if the player inspected another engine earlier.
        if (pawn.IsColonistPlayerControlled && pawn.CanReach(engine, PathEndMode.Touch, Danger.Deadly))
            yield return new FloatMenuOption("ER_InspectEngineRecords".Translate(), () =>
                pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.InspectGravEngine, engine), JobTag.Misc));
    }
}

[HarmonyPatch(typeof(Building_GravEngine), nameof(Building_GravEngine.GetGizmos))]
public static class ShipmentEngineGizmoPatch
{
    public static void Postfix(Building_GravEngine __instance, ref IEnumerable<Gizmo> __result)
    {
        if (!ShipmentRewardPolicy.Marked(__instance)) return;
        // Use our menu action regardless of the global research inspection flag;
        // remove the native inspection button with its misleading unlock text.
        __result = __result.Where(g => !(g is Command_Action action) ||
            action.defaultLabel != "CommandInspectGravEngine".Translate(__instance).Resolve())
            .Concat(new[] { new Command_Action
            {
                defaultLabel = "ER_InspectEngineRecords".Translate(),
                defaultDesc = "ER_EngineRuined".Translate(),
                action = () => Find.Targeter.BeginTargeting(TargetingParameters.ForColonist(), target =>
                {
                    if (target.Pawn != null)
                        target.Pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.InspectGravEngine, __instance), JobTag.Misc);
                })
            } });
    }
}
