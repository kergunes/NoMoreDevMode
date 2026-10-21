using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace EarnedResurrection;

// Mark only objects and sites created for our native subquests. Quest tags are
// vanilla saved fields, so restrictions follow an object through save/load.
internal static class ShipmentRewardPolicy
{
    internal const string SiteTag = "ER_ShipmentRewardSite";
    internal static bool Restricted(Map map)
    {
        if (!(map?.Parent is Site site) || site.questTags?.Contains(SiteTag) != true || Current.Game == null)
            return false;
        // A tag alone is insufficient: verify the parent, its saved pool,
        // the current allow-list, and ownership of this exact site object.
        return Find.QuestManager.QuestsListForReading.Any(child =>
            child.parent?.root?.defName == "ER_Resurrection" &&
            child.parent.GetFirstPartOfType<QuestPart_DlcPool>() != null &&
            DlcLeadCatalog.ContainsRoot(child.root?.defName) &&
            child.PartsListForReading.OfType<QuestPart_SpawnWorldObject>()
                .Any(spawn => ReferenceEquals(spawn.worldObject, site)));
    }

    private static bool IsSpecialLoot(Thing thing)
    {
        switch (thing.def.defName)
        {
            case "Mechlink":
            case "MechSerumResurrector":
            case "MechSerumHealer":
            case "Gravcore":
            case "GravlitePanel":
            case "PsychicAmplifier":
            case "TechprofSubpersonaCore":
            case "AIPersonaCore":
            case "ArchotechArm":
            case "ArchotechEye":
            case "ArchotechLeg":
            case "ArchiteCapsule":
            case "DeathrestCapacitySerum":
            case "OrbitalTargeterBombardment":
            case "OrbitalTargeterPowerBeam":
            case "OrbitalTargeterMechCluster":
            case "Apparel_PsychicInsanityLance":
            case "Apparel_PsychicShockLance":
            case "PsychicAnimalPulser":
            case "PsychicSoothePulser":
            case "Shell_AntigrainWarhead":
            case "TornadoGenerator":
            case "SentienceCatalyst":
            case "BiomutationPulser":
            case "Apparel_BiomutationLance":
            case "PoluxSeed":
            case "HarbingerSeed":
            case "ArcheanSeed":
            case "Map":
                return true;
            case "GravshipShieldGenerator":
            case "PilotSubpersonaCore":
            case "FuelOptimizer":
                // Remove upgrade loot held in crates/minified packages, never
                // a deployed facility that might be part of the site's defenses.
                return thing.ParentHolder != null;
            default:
                return thing is Xenogerm || thing is Genepack || thing.TryGetComp<CompUniqueWeapon>() != null ||
                    thing.def.defName.StartsWith("Techprint_", StringComparison.Ordinal);
        }
    }

    private static void DestroyLoot(Thing thing)
    {
        // A minified package cannot remain with a destroyed InnerThing: vanilla
        // drawing and hauling assume that its contents always exist.
        while (thing.ParentHolder is MinifiedThing package) thing = package;
        if (thing.Destroyed) return;
        // Remove explicitly from a crate/inventory before destroying. Do not
        // leave a destroyed reference in a skyfaller or a casket's ThingOwner.
        thing.holdingOwner?.Remove(thing);
        thing.Destroy(DestroyMode.Vanish);
    }

    internal static void SanitizeMap(Map map)
    {
        if (!Restricted(map)) return;
        var seen = new HashSet<Thing>();
        foreach (Thing thing in map.listerThings.AllThings.ToList()) Visit(thing, seen);
    }

    internal static void SanitizeContainer(Building_Casket casket)
    {
        if (Restricted(casket.Map)) Visit(casket, new HashSet<Thing>());
    }

    private static void Visit(Thing thing, HashSet<Thing> seen)
    {
        if (thing == null || thing.Destroyed || !seen.Add(thing)) return;
        // No pawn, corpse, implant, gene, equipped weapon or carried gear edits.
        if (thing is Pawn || thing is Corpse) return;
        if (thing.ParentHolder is Pawn_EquipmentTracker || thing.ParentHolder is Pawn_InventoryTracker ||
            thing.ParentHolder is Pawn_ApparelTracker || thing.ParentHolder is Pawn_CarryTracker) return;
        // Engine inspection, ownership and research always run vanilla again.
        if (thing.def.defName == "GravEngine") return;
        if (IsSpecialLoot(thing)) { DestroyLoot(thing); return; }
        bool hadCrateContents = thing is Building_Crate originalCrate && originalCrate.HasAnyContents;
        if (thing is IThingHolder holder) VisitHolder(holder, seen, new HashSet<IThingHolder>());
        if (hadCrateContents && thing is Building_Crate crate && !crate.Destroyed && !crate.HasAnyContents)
        {
            // Empty caskets cannot be opened in vanilla. Keep one ordinary scrap
            // so the original opening/hacking/trap signal still fires normally.
            Thing scrap = ThingMaker.MakeThing(ThingDefOf.Steel);
            if (!crate.TryAcceptThing(scrap, false)) scrap.Destroy(DestroyMode.Vanish);
        }
    }

    private static void VisitHolder(IThingHolder holder, HashSet<Thing> seen, HashSet<IThingHolder> holders)
    {
        if (!holders.Add(holder)) return;
        ThingOwner contents = holder.GetDirectlyHeldThings();
        if (contents != null)
            foreach (Thing item in contents.ToList()) Visit(item, seen);
        if (holder is Thing destroyedHolder && destroyedHolder.Destroyed) return;
        var children = new List<IThingHolder>();
        holder.GetChildHolders(children);
        foreach (IThingHolder child in children)
        {
            // Route nested pawns/corpses through the same player exclusions.
            if (child is Thing childThing) Visit(childThing, seen);
            else VisitHolder(child, seen, holders);
        }
    }
}
