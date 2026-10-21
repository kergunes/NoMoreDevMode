# Vanilla research — RimWorld 1.7.4984 rev1242

## Current decision: Ideology primary, varied Core fallback, physical final

This section supersedes the Core-only decision below. The user requested restored
Ideology content, unique encounter selection, configurable lead count and a
physical final cache. Both 1.6.4871 reference signatures and installed 1.7 code
were inspected. `QuestNode_Root_AncientComplex`, `LayoutStructureSketch` and
`ComplexLayoutDef` exist on both targets.

The Ideology pool independently composes an ancient complex, a worshipful terminal
under hostile occupation, and a mechanoid-guarded spacedrone site. It does not call
the actual relic quest roots. The terminal/drone are placed with Core `ItemStash`
plus vanilla defense site parts; this is deliberately an expedition adaptation,
not vanilla's neutral temporary village or home-map spacedrone delivery.
At most three primary encounters are selected before longer chains add Core leads.
Core fallback uses distinct Outpost, SleepingMechanoids, Turrets, Manhunters and
AmbushEdge site parts. Selection removes each chosen type from the candidate pool.

`QuestPart_Dialog` supplies a direct node-tree message after each lead. The final
uses `Building_Casket` / `IOpenable` / vanilla opening jobs. `Building_SerumCache`
gates opening on security clearance, stores the real serum in its native
`ThingOwner`, and only emits its opening result after successful physical ejection.
Destroyed caches never eject their reward. `GenStep_ItemStash` / BaseGen storage
places the case from `SitePart.things`; `SymbolResolver_Stockpile` directly spawns
concrete contents (including buildings) and clears the staging container.
No drop-pod reward part is used in new quests. No player relic is referenced.

## Superseded decision: Core-only encounters (2026-10-22)

The user requested removal of the Ideology requirement. The original research
below remains historical evidence; the current quest no longer uses its complex
or hacking machinery. Inspected Core `Sites/Parts/Outpost.xml`, `BanditCamp.xml`,
`SitePartWorker_Outpost`, `GenStep_Outpost`, and `QuestNode_Root_Mission_BanditCamp`
in the installed game. Core supplies guarded outposts, point-scaled Settlement
defenders, power generation and the Site `AllEnemiesDefeated` signal. These are
the replacement for the three leads and final, with no copied DLC assets.

`SitePartWorker_Outpost.GenerateDefaultParams` applies the faction's minimum
Settlement budget and a loot curve. Our sites set `lootMarketValue` to zero;
`GenStep_Outpost` passes that value to BaseGen. The quest's deterministic final
serum is still delivered through `Reward_Items`/`QuestPart_DropPods`.

No `QuestNode_Root_AncientComplex`, `CompHackable`, `AncientTerminal`, complex
loot-table or Ideology-active check is used in newly generated quests. The only
optional DLC XML is the guarded legacy `ER_AncientComplex` definition for saved
older quests. It remains available when Ideology is enabled and is skipped when
absent. There is no migration of active older quests. All DLC are optional;
Harmony remains required for the same narrow death patch.

## Historical Ideology implementation research

Inspected the installed `Version.txt`, `RimWorld.runtimeconfig.json`, `Assembly-CSharp.dll`
with ILSpy, and Core/Ideology XML on 2026-10-21. Decompiled game code remains local
and ignored. Target: Waylith, .NET 10, Core + Ideology. No 1.6 compatibility claim.

## Findings and reuse decision

| Vanilla implementation | Finding / decision |
| --- | --- |
| `QuestNode_Root_RelicHunt`, `Script_RelicHunt.xml` | Five successful leads via `QuestPart_SubquestGenerator_RelicHunt`, 5–10 day intervals, then `AncientAltar`. Calls `Precept_Relic.GenerateRelic()`. Do not invoke. |
| `QuestPart_SubquestGenerator` | Generic persisted interval/subquest accounting, but deliberately retries unsuccessful subquests. Our fail-once design instead uses one quest with sequential signal-gated stages. |
| `QuestNode_Root_Hack_AncientComplex` | Falls back to a real player relic when no relic is supplied; creates `Reward_RelicInfo`. Unsafe to call directly. |
| `QuestNode_Root_AncientComplex` | Safe abstract base: reuse `GenerateStructureSketch`, the vanilla ancient layout's rooms/worker, size/terminal curves and `ThreatPointsOverPointsCurve`. No relic state in these methods. Our inherited layout excludes security-crate threats whose separate loot table can roll serum. |
| `CompHackable`, `QuestPart_PassAllActivable`, `QuestPart_Filter_Hacked` | Reuse terminal `.Hacked` and `.Destroyed` quest tags; persisted all-terminal completion and destruction filtering. |
| `QuestNode_Root_Hack_WorshippedTerminal` | Requires a real relic, generates temporary faction and relic-info reward. Excluded from V0.1; would need independent wrapper. |
| `QuestNode_Root_Hack_Spacedrone` | Relic-info reward and real-relic fallback; home-map encounter with raids. Excluded rather than introducing fake relics. |
| `GenStep_AncientAltar`, `SymbolResolver_AncientAltar`, `SitePartWorker_AncientAltar` | Uses reliquary, `relicThing`, extraction security and relic-lost signals. Even though its inner container can hold a generic item, labels and completion are relic-specific and extraction need not defeat enemies. Excluded. |
| `AncientComplex`, `SleepingMechanoids` site parts | Reuse vanilla map generators, ancient complex threats/loot, and point-scaled exterior defenders for final combat. No custom generator. |
| `QuestGen_Sites.GenerateSite`, `QuestPart_SpawnWorldObject` | Reuse world sites, quest tags and deferred spawn signals. Sites appear one at a time. |
| `QuestPart_Delay`, `QuestPart_WorldObjectTimeout`, `QuestPart_QuestEnd` | Vanilla saved delays, site cleanup, success/failure. Stages have a generous 30-day deadline and chain failure is permanent. |
| `Reward_Items`, `QuestPart_DropPods` | Display precisely one `MechSerumResurrector`; deliver that same item once after final objective, then end successfully. No random reward generator. |
| `Pawn.Kill` | No general subscribable persistent-component death event found. A narrow Harmony prefix snapshots eligibility; postfix notifies only after a living-to-dead transition. Map and caravan deaths share this path. |
| `Pawn_PlayerSettings.joinTick` | Constructor timestamp, not reliable duration of free colonist status (components can exist for other statuses). Do not use it as membership proof. |
| `RecordDefOf.TimeAsColonistOrColonyAnimal`, `RecordWorker_TimeAsColonistOrColonyAnimal` | Vanilla saved cumulative ticks; excludes slaves, prisoners and extra home faction guests. Reuse for 20 days of actual membership, with strict current ownership checks. No parallel join ledger. |

## Independent resurrection chain

One new quest root composes **three ancient-complex information expeditions** and
a fourth, more strongly defended complex. Repeating the safe ancient-complex
encounter is intentional for this easy version; no new encounter archetype.
Final completion requires all terminals hacked **and no remaining hostile threats,
including dormant defenders**. The generic serum arrives by vanilla pod at the
final site. It never enters a reliquary or receives `CompRelic`/precept state.

Quest signals and saved vanilla quest parts own progression. A small saved final
objective part checks current threats on terminal/clearance signals to avoid
treating an earlier, stale clearance signal as final victory. A cleanup part
reports the quest outcome to the persistent game component and removes our sites.

No `Precept_Relic`, `GenerateRelic`, `Reward_RelicInfo`, relic history event,
development-points reward, or player ideology mutation is used by this mod.
If the named pawn's corpse disappears or they resurrect elsewhere, the chain
continues; the serum's behavior remains entirely vanilla.
