# Architecture

## Versioned loading

One source project builds either `RimWorldVersion=1.6` (net472, pinned RimRef
1.6.4871 and matching local Harmony) or `1.7` (net10.0, local installed game
and matching Harmony). The default is 1.7. Settings use Framework-compatible
`Math.Min/Max` and non-generic `Enum.IsDefined` on both targets.

`LoadFolders.xml` loads `Common` plus the corresponding `1.6`/`1.7` directory;
the mod assembly is present once per selected version. Shared quest XML and
languages live in `Common`. Root-level `Assemblies/` is excluded. The optional
`ER_AncientComplex` layout lives in `Common/Defs` and is skipped without Ideology.
Existing supported save field names are unchanged. Runtime
acceptance is pending on both targets; this does not support save downgrades.

## Death and membership

`ResurrectionMod` installs one Harmony patch, on `Pawn.Kill`. Its prefix snapshots
eligibility while the pawn still belongs to its map/caravan. Its postfix only
notifies after that previously living eligible pawn is dead. Repeated calls on an
already dead pawn cannot qualify. Resurrection restores eligibility for a later
real death, subject to the same global opportunity limits. Destroying a living
pawn without `Kill` is not a qualifying death.

Eligibility requires humanlike, player faction, free colonist, no prisoner/slave,
no host faction, no quest lodger/extra home faction, and presence in vanilla's
`PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists_NoLodgers`.
Mental breaks and being downed do not disqualify established colonists. Off-map
world faction members do not qualify. The vanilla saved
`TimeAsColonistOrColonyAnimal` record must meet the configured minimum: 0–60 days,
default 20 (1,200,000 ticks). This
is **cumulative actual membership**, not continuous membership or biological age;
former colonists retain legitimately earned membership time. Its worker excludes
prisoner, slave and extra home faction time. No separate membership ledger.

## Save state and scheduling

`ResurrectionState : GameComponent` is one instance per game, across all maps and
caravans. It scribes `nextEligibleTick`, `pendingTick`, `chainActive`, `activeQuest`,
and the triggering pawn's immutable narrative name/ID and death tick. No corpse
reference is required. The generated quest description owns its narrative text.

Eligible death -> one pending timestamp using the selected arrival setting:
immediate (next tick), randomly 60,000–120,000 ticks (default), or randomly
60,000–420,000 ticks (within one week).
Any death while pending, active or before `nextEligibleTick` is discarded immediately.
A cheap due-time comparison runs per game tick; due events are processed on that
tick, outside the death callback. Idle reconciliation of a missing/historical quest
runs every 250 ticks. It does not scan or tick pawns. No catch-up death queue.
If violent quests are disabled or no suitable ground site exists when due, the
opportunity is discarded without a cooldown; a future death is required.

The pending timestamp is cleared before generation. A successful generated quest
is assigned as active and starts the configured cooldown (0–120 days, default
60 / 3,600,000 ticks) before being
registered with the quest manager and announced. Offer expiration also consumes
the opportunity. No failure path shortens the cooldown. The generated end tick
is saved unchanged when preferences later change. At zero days, pending and
active-chain guards still prevent overlap and no old death queue is created.

`Scribe_Values`, `Scribe_References` and normal QuestManager serialization own all
saved state. Loaded-game reconciliation clears an orphan active reference and
retains cooldown. No mutable static gameplay state or custom serializer.

`ResurrectionSettings : ModSettings` persists the arrival enum, minimum membership days
and whole days between leads (0–7, default 1)
using vanilla mod preferences. `ResurrectionMod` exposes radio buttons and a
whole-day slider through the standard settings window. The static settings
reference is a user preference object, not world/save state. Changing settings
affects future deaths only; existing pending timestamps remain unchanged.

## DLC dispatch and saved encounter plan

`DlcLeadCatalog` is an explicit 17-entry allow-list, gated by active DLC and four
default-true preferences plus a saved list of individually disabled encounter
names (empty by default). Settings show expandable per-DLC encounter lists.
No Anomaly/orbital/gravcore/boss/cluster root is enumerated.
Mechanitor complex, gravship wreckage and mechanoid signal were removed for
overlapping gameplay and bespoke reward complexity. Dispatch intersects saved
allowed names with the current catalog, so even old parent pools cannot choose
these removed roots for a future lead. Existing generated children and marked
objects retain their logic/protections; no active site or item is deleted.
The problem-causer adapter runs a temporary native sequence without the mechanoid
selection node; shared vanilla defs are never modified. Hospitality runs its native
peaceful difficulty plus an explicit human raid to avoid randomized mech clusters.

`QuestPart_DlcPool` saves allowed/used names on the parent at generation. Each lead
reserves a different Core fallback; `QuestPart_DlcLead` tries weighted, unused,
currently runnable DLC roots first. It runs on the next tick after activation,
outside QuestGen's non-reentrant generation pass. Native prerequisites are checked
with `CanRun`; no eligible root means activating the reserved Core fallback.
Each child saves its parent link, and the dispatcher saves its child reference,
started flag and 30-day deadline. It relays success once through the existing
progress dialog/delay; child failure, offer expiration, unknown abandonment or
outer timeout fails the parent. Parent cleanup concludes any unfinished child.

Native reward choices/parts are removed before registration; ordinary physical site
loot remains. Opportunity sites keep native layouts and occupants but replace premature
arrival/claim success with a guarded shipment-archive interaction. That archive
holds a component, never serum. Crash/hosting combat gates preserve the native
success prerequisite and also require hostile clearance, including dormant enemies.
Native mission deadlines can be shorter than the outer 30 days. Surface-only
validation prevents dispatching an orbital or underground expedition.

### Shipment-only physical reward restrictions

`ShipmentRewardPolicy.GenerationScope` is a thread-local, disposable capture active
only around this dispatcher's `QuestGen.Generate` call. Harmony postfixes record
new things/pawns only while it is active; no global quest root or def is modified.
After generation, it marks engines/unique weapons, removes the sanguophage's
XenogermReimplanter gene (retaining combat genes), and removes special loose loot.
The collector is discarded in `Dispose` even if generation fails.

New child sites receive the saved `ER_ShipmentRewardSite` quest tag before spawning.
`Site.PostMapGenerate` traverses their nested holders once, removes dead mechanitors'
MechlinkImplant, cleans special loot and updates mechlink reveal messages without
changing casket opening signals. `CompLootSpawn.PostSpawnSetup` cleans later crate
contents on those marked sites. Player pawns/corpses and their carried gear are
excluded. Minified packages are removed whole to avoid empty-package references.
If cleaning empties a previously occupied crate, one steel scrap preserves vanilla
CanOpen and its original opening/hacking/trap signals; already empty crates stay empty.

Special crate loot includes serums, mechlinks, unique weapons, archotech parts,
gene/archite rewards, psychic/orbital devices, grav resources/upgrades, techprint
and quest-map rewards. Ordinary supplies/equipment/resources remain. Deployed
gravship facilities are retained; only packaged facility rewards are removed.
Enemy-equipped unique weapons keep every combat trait and acquire a saved object
tag; equipment drop/transfer patches destroy only marked unique weapons. Ruined
engines keep their original object/tag/inspection job and emit `Inspected`, but
their Inspect prefix bypasses research, ownership and gravship-quest generation.
Claim/launch reports reject only marked engines; their record-reading action stays
available even after an unrelated engine has been inspected.

The parent final site has no restriction tag, so its physical serum is untouched.
Ordinary vanilla quests have neither our generation scope nor tags and take the
original code path. No old lead or already acquired reward is migrated/confiscated.
Compilation is verified; in-game equivalence is explicitly pending human checks.

The distant Ideology shuttle mission requires Royalty and a real player relic for
the native text/sketch setup. Its `Reward_RelicInfo` is removed before registration;
it is parented only to this mod's quest, never to the relic hunt. Existing independent
Ideology encounters do not need any player relic. Final generation remains on the
parent, and respects the saved-at-generation Ideology pool choice.

The root uses vanilla quest primitives with no player relic/precept state. At
quest generation it freezes the selected 1–5 lead count (default 3), the 0–7 day
interval, faction/threat budgets and encounter choices into native quest parts.
It then appends one separate final recovery. Existing quests are not rebuilt.

Selection records each chosen DLC root in the shared used list; no type repeats.
Independent ancient complex, guarded worshipful terminal and defended spacedrone
are Ideology entries alongside work sites and the distant mission. The Core
fallback pool is occupied outpost, sleeping mechanoids, turrets, manhunters and edge ambush.
Combat weights favor defended sources over the animal encounter. A world with
insufficient supported encounter types/factions cannot generate the requested
unique plan; no duplicated filler is silently inserted.

All sites use vanilla generators. Complexes use the optional independent
ER_AncientComplex layout (security crates excluded to avoid random extra serum).
Shrine/drone expeditions reuse their Ideology hackable objects, placed through
ItemStash plus vanilla defenders; they do not invoke relic-dependent quest roots.
Core leads use their corresponding defense site part and a small deterministic
supply stash. Random outpost loot value is zero. Site/letter names describe the
encounter; sequence indices remain internal signals only.

QuestPart_ArchiveObjective listens to saved hacking and AllEnemiesDefeated
signals. It requires every configured terminal hacked and no active threat,
including dormant/fogged enemies. Core leads have no terminal requirement. It
completes once through QuestPartActivable's saved state. Destroying an unhacked
terminal, abandoning an unfinished site, site destruction or a 30-day deadline
fails the chain. Completed stages disable those failure gates.

Each lead completion opens a vanilla QuestPart_Dialog directly. A gated waiting
paragraph and native delay timer stay in the quest UI until the next site is
revealed. Zero-day intervals use different text without promising a wait. A
scrollable settings window accommodates all preferences.

## Physical final and cleanup

A final always follows, regardless of Ideology or lead count. With Ideology it
uses an ancient complex with additional guards; Core fallback uses an outpost.
Both include an ItemStash containing one Building_SerumCache. Final generation
points are multiplied by 1.5. Defense budgets use vanilla faction minimums.

One vanilla MechSerumResurrector is made at generation and placed in that cache's
native deep-saved ThingOwner. Reward_Items displays a reference to the same item;
no reward generator, duplicate item, or QuestPart_DropPods is created. Opening is
locked until current threats (including dormant/fogged enemies) are cleared.

Building_SerumCache inherits Core Building_Casket and IOpenable, so the player
orders the normal opening job. Open ejects the serum nearby; only an empty
container after ejection emits the saved success signal. Failed placement does
not resolve success. Reopening an empty cache cannot emit another result.
Destroying/deconstructing the case clears contents before base destruction, so
it cannot bypass opening. The quest fails if the case is destroyed before success.

QuestPart_ResurrectionCleanup retains a cache reference. On failure/expiry it
clears the undelivered contents, reports outcome to the global state component
and removes/marks sites for cleanup. On success the physically recovered serum
is left alone. The cooldown is never refunded. Collect the serum before leaving.
The older QuestPart_SerumRecovery type remains for old saved quests only.

## Save compatibility and acceptance

Shared save field names are retained, including old quest parts. Existing chains
keep their old objectives, delays and reward mechanism; changes apply to newly
generated chains. Do not remove Ideology from a save containing its active sites.
No cross-version save downgrade support or automatic active-chain migration.

Both targets are compiled; game loading, XML resolution, individual generators,
opening jobs, dialogs and persistence still require human verification. No game
was launched for this revision at the user's request.
