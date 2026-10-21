using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace EarnedResurrection;

// Mark only objects and sites created for our native subquests. Quest tags are
// vanilla saved fields, so restrictions follow an object through save/load.
internal static class ShipmentRewardPolicy
{
    internal const string SiteTag = "ER_ShipmentRewardSite";
    internal const string ObjectTag = "ER_ShipmentRewardObject";
    internal const string InspectedTag = "ER_ShipmentEngineInspected";
    [ThreadStatic] internal static GenerationScope CurrentGeneration;

    internal sealed class GenerationScope : IDisposable
    {
        private readonly GenerationScope previous;
        internal readonly List<Thing> things = new List<Thing>();
        internal GenerationScope()
        {
            previous = CurrentGeneration;
            CurrentGeneration = this;
        }
        public void Dispose() => CurrentGeneration = previous;

        internal void Apply(string source)
        {
            foreach (Thing thing in things.Distinct().ToList())
            {
                if (thing == null || thing.Destroyed) continue;
                if (thing is Pawn pawn)
                    SanitizePawn(pawn, source == "SanguophageShip");
                else if (IsEngine(thing) || IsUnique(thing))
                    Mark(thing);
                else if (IsSpecialLoot(thing))
                    DestroyLoot(thing);
            }
        }
    }

    internal static bool Restricted(Map map) => map?.Parent?.questTags?.Contains(SiteTag) == true;
    internal static bool Marked(Thing thing) => thing?.questTags?.Contains(ObjectTag) == true;
    internal static bool IsEngine(Thing thing) => thing?.def.defName == "GravEngine";
    internal static bool IsUnique(Thing thing) => thing?.TryGetComp<CompUniqueWeapon>() != null;

    internal static void Mark(Thing thing)
    {
        if (thing.questTags == null) thing.questTags = new List<string>();
        if (!thing.questTags.Contains(ObjectTag)) thing.questTags.Add(ObjectTag);
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
                return thing is Xenogerm || thing is Genepack ||
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

    internal static void SanitizePawn(Pawn pawn, bool removeImplanter = false)
    {
        if (pawn == null || pawn.Faction == Faction.OfPlayer || pawn.IsQuestLodger()) return;
        if (pawn.Dead && pawn.kindDef.defName == "Mechanitor_Basic")
        {
            foreach (Hediff implant in pawn.health.hediffSet.hediffs
                         .Where(h => h.def.defName == "MechlinkImplant").ToList())
                pawn.health.RemoveHediff(implant);
        }
        if (removeImplanter && pawn.genes != null)
        {
            // Keep deathless/hemogen/combat genes and the NPC's combat behavior.
            // Only the ability that gives the player a transferable xenogerm goes.
            foreach (Gene gene in pawn.genes.GenesListForReading
                         .Where(g => g.def.defName == "XenogermReimplanter").ToList())
                pawn.genes.RemoveGene(gene);
        }
        if (pawn.equipment == null) return;
        foreach (ThingWithComps weapon in pawn.equipment.AllEquipmentListForReading)
            if (IsUnique(weapon)) Mark(weapon);
    }

    internal static void SanitizeMap(Map map)
    {
        if (!Restricted(map)) return;
        var seen = new HashSet<Thing>();
        foreach (Thing thing in map.listerThings.AllThings.ToList()) Visit(thing, seen);
        foreach (SignalAction_Letter letter in map.listerThings.AllThings.OfType<SignalAction_Letter>())
        {
            if (letter.letterLabelKey != "LetterLabelMechanitorCasketOpened" &&
                letter.letterLabelKey != "LetterLabelMechanitorCasketFound") continue;
            letter.letterLabelKey = "ER_ArchiveRemainsLabel";
            letter.letterMessageKey = "ER_ArchiveRemainsText";
        }
        foreach (SignalAction_Message message in map.listerThings.AllThings.OfType<SignalAction_Message>())
            if (message.signalTag?.StartsWith("MechanitorCasketOpened", StringComparison.Ordinal) == true)
                message.message = "ER_ArchiveRemainsText".Translate().Resolve();
    }

    internal static void SanitizeContainer(Building_Casket casket)
    {
        if (Restricted(casket.Map)) Visit(casket, new HashSet<Thing>());
    }

    private static void Visit(Thing thing, HashSet<Thing> seen)
    {
        if (thing == null || thing.Destroyed || !seen.Add(thing)) return;
        if (thing is Pawn player && player.Faction == Faction.OfPlayer) return;
        if (thing is Corpse corpse && corpse.InnerPawn.Faction == Faction.OfPlayer) return;
        // Player gear brought into the expedition is never reward loot.
        if (thing.ParentHolder is Pawn_EquipmentTracker equipment && equipment.pawn.Faction == Faction.OfPlayer) return;
        if (IsEngine(thing)) { Mark(thing); return; }
        if (IsUnique(thing))
        {
            if (thing.ParentHolder is Pawn_EquipmentTracker) Mark(thing);
            else DestroyLoot(thing);
            return;
        }
        if (IsSpecialLoot(thing)) { DestroyLoot(thing); return; }
        if (thing is Pawn pawn) SanitizePawn(pawn);
        if (thing is Corpse remains) Visit(remains.InnerPawn, seen);
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
