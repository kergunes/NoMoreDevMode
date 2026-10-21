using HarmonyLib;
using RimWorld;
using RimWorld.Planet;

namespace EarnedResurrection;

// Retired engine/pawn/equipment patches are intentionally absent. Only verified
// shipment-owned sites can enter the remaining physical loot filters.
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
