# Vanilla quest isolation audit — 2026-10-22

Evidence: source review, installed official XML inspection, production compilation
and metadata inspection of both generated DLLs. No game was launched or gameplay
scenario executed. Runtime equivalence remains HUMAN VERIFICATION REQUIRED.

## Removed from the shipment pool, still present in vanilla

| Native quest root | Installed vanilla definition | Selectable by this mod |
| --- | --- | --- |
| OpportunitySite_AncientComplex_Mechanitor | Present | No |
| MechanoidSignal | Present | No |
| GravshipWreckage | Present | No |
| SanguophageShip | Present | No |
| OpportunitySite_AncientMercenaries | Present | No |

Official XML was read from the installed Biotech/Odyssey QuestScriptDefs folders.
The mod neither removes these defs nor changes their selection weights or roots.
Its only selection change is the local DlcLeadCatalog list. Saved parent pools are
intersected with that current list, so they cannot reintroduce retired roots.

## Compiled Harmony targets

Metadata in BOTH 1.6 and 1.7 production DLLs contains exactly these patch classes:

- ColonistDeathPatch — Pawn.Kill, existing free-colonist death scheduling.
- ShipmentSiteLootPatch — Site.PostMapGenerate, restricted shipment-site loot.
- ShipmentCrateLootPatch — CompLootSpawn.PostSpawnSetup, restricted crate loot.

Engine inspection/claim/launch/UI, weapon drop/transfer, ThingMaker and
PawnGenerator patch classes are absent from both DLLs. There is no generation
capture, implant removal, gene removal or mechanitor reveal-message rewriting.
Unused old localization keys remain for compatibility with serialized old text;
no executable handler uses them to send new engine/mechanitor messages.

## Remaining filters and messages

ShipmentRewardPolicy.Restricted requires ALL of:

1. The map belongs to a Site with the shipment-site tag.
2. Its owning child has a parent rooted at ER_Resurrection with QuestPart_DlcPool.
3. The child root is in the CURRENT catalog, including the human problem-causer alias.
4. The child's SpawnWorldObject part references this exact Site instance.

A normal vanilla instance of the SAME native root fails the ownership checks.
A retired child fails the current-catalog check. A stale tag alone is insufficient.
The two filters do not send letters/messages, change quest outcomes or replace
opening/hacking signals. Pawn/corpse/carried equipment and engines are excluded.
Crates emptied by filtering receive one steel scrap to preserve CanOpen/traps.
The parent final serum site does not qualify as a child site.

Quest-specific text, success/failure parts and reward choices are changed only on
the newly generated child Quest object in QuestPart_DlcLead.PrepareChild. Shared
vanilla QuestScriptDef/root trees are not edited. The human problem-causer adapter
uses a separate node list and per-generation Slate rather than editing native nodes.
No XML PatchOperation removes or disables native quest definitions.

## Save and verification limits

Restart RimWorld completely to unload hooks installed by the previous DLL.
Existing saved QuestPart classes remain available; active quests/maps/items are
not deleted. Previously saved item, implant, gene or resolved-text changes are not
automatically reconstructed. Fresh ordinary vanilla quests are the comparison
baseline; use docs/TESTING.md for the in-game checks still awaiting the user.
