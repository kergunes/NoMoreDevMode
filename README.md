# NoMoreDevMode / Earned Resurrection

V0.1 for RimWorld 1.6 / 1.7 + Harmony. **No DLC required.** Resurrection should not be random. It should be earned.

## What it does

An established player colonist dies. After a configurable delay the colony can receive a
chain of distinct leads followed by a more strongly defended
serum recovery expedition. Open its sealed cache to recover exactly one vanilla
resurrector mech serum. The mod changes access to the item, never resurrection.

## Core rules

- Minimum cumulative actual free-colonist membership: configurable 0–60 days, default 20, using vanilla records.
- Quest arrival: immediately (next game tick), within 1–2 days (default), or within one week (1–7 days).
- Global cooldown: configurable 0–120 days, default 60, beginning when the opportunity is generated. Existing cooldown end times are preserved when settings change.
- One pending opportunity or active chain across the entire save.
- Deaths during pending/active/cooldown periods are discarded, never queued.
- Failure or an expired/abandoned expedition consumes the opportunity; no retries.
- Leads before the final: configurable 1–5, default 3; the final is always separate.
- No encounter type repeats within a chain. Sites have descriptive names without stage numbers.
- Eligible, unused DLC encounters are selected before Core encounters, with combat encounters favored over hospitality. Royalty, Ideology, Biotech and Odyssey each have an enabled-by-default settings checkbox and expandable list of individual encounter checkboxes, shown only when that DLC is active; a questline saves its pool when generated.
- Royalty: shuttle assault, human-held problem-causer outpost, shuttle rescue, diverted raids and protected hospitality. Ideology: independent archive/shrine/drone encounters, work camp and distant shuttle mission (the latter also needs Royalty and a player relic for native setup, without advancing its hunt).
- Biotech: mechanitor complex and sanguophage crash. Odyssey: mercenary bunker, garrison, launch site, refinery, warehouse, infested settlement, grounded gravship wreck and downed mechanoid signal. Native prerequisites still apply; unavailable roots are skipped.
- No mech-cluster quest, mechanitor ship, mechanitor boss, Anomaly, gravcore-chain or orbital mission is selected. The problem-causer quest is restricted to its human outpost branch; hospitality uses an explicit human raid instead of native randomized threats. Space travel is never required.
- When no eligible unused DLC encounter remains, occupied depot, sleeping mechanoid yard, automated turret relay, manhunter-overrun waystation and ambushed rendezvous form the Core fallback pool. With every DLC pool disabled, all leads use Core.
- Time between leads: configurable 0–7 whole days, default 1. Each recovered lead opens a direct message dialog and shows the remaining wait in the quest UI.
- Each lead has a 30-day outer deadline; native DLC operations can impose shorter offer/mission deadlines. The parent offer expires after ten days. Native DLC leads appear as linked subquests and may need separate acceptance.
- Final success requires all threats cleared, including dormant/fogged enemies.
- Reward: one generic serum physically stored in a sealed medical cache at the final site. Defeat its defenders, then order a colonist to open it. No reward pod. Destroying the cache destroys its contents.
- No changes to actual ideology relic progression, corpse preservation, serum use or sickness.
- Corpse destruction or resurrection elsewhere does not cancel the chain.
- No opportunity while violent quests are disabled, or when no suitable expedition site exists.

## Requirements and installation

Targets: **1.6.4871 / .NET Framework 4.7.2** (reference-assembly build) and
**1.7.4984 rev1242 / Waylith / .NET 10** (installed-game build).
Requires **Core and the matching Harmony version**. All DLC are optional.
Enable Harmony before Core, then **NoMoreDevMode / Earned Resurrection**.
Do not enable the separate development-only integration test mod for normal play.

Already generated quests from the older Ideology version retain their original
complexes and terminal objectives. Finish them with Ideology still enabled before
removing that DLC. New quests use the enabled DLC pools, or Core fallback otherwise.
This update does not migrate old active quests or make arbitrary DLC removal from
an existing save safe.

Build, then copy the package root (`About`, `Common`, `1.6`, `1.7`, `LoadFolders.xml`) into a folder
under RimWorld's `Mods/`, or use a local directory junction. Do not distribute game
DLLs or Harmony's DLL with this mod. No preview image is included yet.

`LoadFolders.xml` loads shared content from `Common` and exactly one version's
assembly directory. The old root `Assemblies/` is deliberately not loaded, so a
stale earlier DLL cannot be loaded alongside the current one. The optional
Ideology layout in `Common/Defs` is skipped when the DLC is absent on either version.
This is mod support for both versions, not support for downgrading a 1.7 game save.

## Verification status

Both production DLLs compiled with zero warnings/errors. The 1.6 build uses
Krafs.Rimworld.Ref 1.6.4871 and local Harmony 1.6; a full 1.6 game installation
was not available. The 1.7 build uses the installed game and local Harmony 1.7.
No gameplay test
has been run for the current varied-encounter/cache-interaction version. Historically, an isolated
game launch loaded the older Ideology version, but the automated integration attempt stopped
on its Core+Ideology-only fixture assertion: RimWorld auto-enabled newly discovered
owned DLC in the fresh test profile. The requested complete chain/save-load/reward
tests have **not passed**. Further automated testing was stopped at the user's request.

**HUMAN VERIFICATION REQUIRED. This is not yet an accepted V0.1 release.**
See [the manual checklist](docs/TESTING.md), [architecture](docs/ARCHITECTURE.md)
and [vanilla research](docs/VANILLA_RESEARCH.md).

## Build

Requires a .NET SDK supporting .NET 10 (both commands work with the installed
.NET 10 SDK). Build the 1.6 target using pinned NuGet reference assemblies:

```powershell
dotnet build -c Release -p:RimWorldVersion=1.6 -p:Harmony16Assembly="your Harmony mod/1.6/Assemblies/0Harmony.dll"
```

The project restores `Krafs.Rimworld.Ref` 1.6.4871 and .NET Framework reference
assemblies; no installed 1.6 game or runtime is needed to compile. These are
compile-time references, not game runtime DLLs to distribute. See the
[reference package](https://www.nuget.org/packages/Krafs.Rimworld.Ref/1.6.4871).

Build 1.7 (the default target) against your installed Waylith game:

```powershell
$env:RIMWORLD_DIR = 'your RimWorld installation directory'
$env:HARMONY_ASSEMBLY = 'your Harmony mod/1.7/Assemblies/0Harmony.dll'
dotnet build -c Release
```

Alternatively pass `-p:RimWorldDir="..." -p:HarmonyAssembly="..."`, or create the ignored `Directory.Build.props.user`:

```xml
<Project><PropertyGroup>
  <RimWorldDir>your game directory</RimWorldDir>
  <Harmony16Assembly>your Harmony 1.6 assembly</Harmony16Assembly>
  <Harmony17Assembly>your Harmony 1.7 assembly</Harmony17Assembly>
</PropertyGroup></Project>
```

The DLLs are written to `1.6/Assemblies/` and `1.7/Assemblies/`. Each command builds
one target. With both built, run `./scripts/Package.ps1` to create an installable
ZIP under `artifacts/`, containing only this mod's binaries and content.

Open Options → Mod settings → **NoMoreDevMode / Earned Resurrection** to choose
quest arrival time, lead count, days between leads, minimum membership days and
global cooldown days, and the enabled DLC encounter pools. Settings persist through vanilla
`ModSettings` and are global preferences, not per-save state. They apply to future
deaths/questlines; pending arrival times, active quests and cooldowns are not
rescheduled. Stage intervals are fixed when a questline is generated, so changing
the interval does not alter an existing chain.
English and Turkish settings labels are included. Other gameplay defaults remain
centralized in `ResurrectionTuning.cs`.

## Scope

No custom map generator, resurrection item, convoy AI, bespoke DLC bosses,
compatibility layers or multiplayer support. DLC leads reuse native quest roots
as linked subquests, retaining their layouts and operation logic. Opportunity
sites require clearing defenders and opening a shipment archive; home crash
quests also require the native operation to finish and surviving threats to be
cleared. Ordinary generated reward choices are removed: the lead reward is
information, and the separate final supplies the serum. Ordinary native salvage
remains, but special progression loot is restricted on newly generated shipment
leads: no corpse mechlink, transferable sanguophage xenogerm, usable grav engine,
unique weapon reward, bonus serum, or special crate upgrade. Enemy unique weapons
retain their combat effects and are destroyed when dropped/transferred. Ruined
engines retain inspection as a quest action without research/ownership unlocks.
Normal vanilla quests and the final serum cache are outside these restrictions.
Already generated old leads/items are not retroactively changed.
