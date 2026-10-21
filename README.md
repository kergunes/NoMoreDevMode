# NoMoreDevMode / Earned Resurrection

V0.1 for RimWorld 1.7 + Harmony. **No DLC required.** Resurrection should not be random. It should be earned.

## What it does

An established player colonist dies. After a configurable delay the colony can receive a
three-lead enemy outpost quest chain followed by a more strongly defended
serum recovery expedition. Successful completion delivers exactly one vanilla
resurrector mech serum. The mod changes access to the item, never resurrection.

## Core rules

- Minimum cumulative actual free-colonist membership: configurable 0–60 days, default 20, using vanilla records.
- Quest arrival: immediately (next game tick), within 1–2 days (default), or within one week (1–7 days).
- One global 60-day cooldown, beginning when the opportunity is generated.
- One pending opportunity or active chain across the entire save.
- Deaths during pending/active/cooldown periods are discarded, never queued.
- Failure or an expired/abandoned expedition consumes the opportunity; no retries.
- Secure three enemy outposts to recover shipment records, then a stronger final outpost; one day between leads.
- Each discovered site has a 30-day deadline. An unaccepted offer expires after ten days.
- Final success requires all threats cleared, including dormant/fogged enemies.
- Reward: one generic serum, delivered by vanilla pod at the final site. Collect it before leaving.
- No changes to actual ideology relic progression, corpse preservation, serum use or sickness.
- Corpse destruction or resurrection elsewhere does not cancel the chain.
- No opportunity while violent quests are disabled, or when no suitable expedition site exists.

## Requirements and installation

Target verified from installed files: **RimWorld 1.7.4984 rev1242**, Waylith/.NET 10.
Requires **Core and Harmony's 1.7 build**. Ideology and all other DLC are optional.
Enable Harmony before Core, then **NoMoreDevMode / Earned Resurrection**.
Do not enable the separate development-only integration test mod for normal play.

Already generated quests from the older Ideology version retain their original
complexes and terminal objectives. Finish them with Ideology still enabled before
removing that DLC. New quests always use Core outposts, with or without Ideology.
This update does not migrate old active quests or make arbitrary DLC removal from
an existing save safe.

Build, then copy this mod root (About, Assemblies, Defs, Languages) into a folder
under RimWorld's `Mods/`, or use a local directory junction. Do not distribute game
DLLs or Harmony's DLL with this mod. No preview image is included yet.

## Verification status

Production DLL compiled against the installed game and Harmony; no gameplay test
has been run for the DLC-independent outpost version. Historically, an isolated
game launch loaded the older Ideology version, but the automated integration attempt stopped
on its Core+Ideology-only fixture assertion: RimWorld auto-enabled newly discovered
owned DLC in the fresh test profile. The requested complete chain/save-load/reward
tests have **not passed**. Further automated testing was stopped at the user's request.

**HUMAN VERIFICATION REQUIRED. This is not yet an accepted V0.1 release.**
See [the manual checklist](docs/TESTING.md), [architecture](docs/ARCHITECTURE.md)
and [vanilla research](docs/VANILLA_RESEARCH.md).

## Build

Requires the .NET 10 SDK and a local RimWorld 1.7 installation (Waylith).
The installed game's runtime is .NET 10; this project intentionally targets it.

```powershell
$env:RIMWORLD_DIR = 'your RimWorld installation directory'
$env:HARMONY_ASSEMBLY = 'your Harmony mod/1.7/Assemblies/0Harmony.dll'
dotnet build -c Release
```

Alternatively pass `-p:RimWorldDir="..." -p:HarmonyAssembly="..."`, or create the ignored `Directory.Build.props.user`:

```xml
<Project><PropertyGroup>
  <RimWorldDir>your game directory</RimWorldDir>
  <HarmonyAssembly>your Harmony 1.7 assembly</HarmonyAssembly>
</PropertyGroup></Project>
```

The DLL is written to `Assemblies/`. Game assemblies are referenced locally and never distributed.
Open Options → Mod settings → **NoMoreDevMode / Earned Resurrection** to choose
quest arrival time and minimum membership days. Settings persist through vanilla
`ModSettings` and are global preferences, not per-save state. They apply to future
deaths; pending arrival times, active quests and cooldowns are not rescheduled.
English and Turkish settings labels are included. Other gameplay defaults remain
centralized in `ResurrectionTuning.cs`.

## Scope

Easy V0.1 only: no custom map generator, resurrection item, convoy AI, DLC-specific
finals, compatibility layers or multiplayer support. The three leads intentionally
reuse the same vanilla Core outpost encounter. Future V0.2 work can explore
DLC-aware final encounters and more varied independent questlines after this loop
passes gameplay acceptance.
