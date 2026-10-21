# Architecture

## Versioned loading

One source project builds either `RimWorldVersion=1.6` (net472, pinned RimRef
1.6.4871 and matching local Harmony) or `1.7` (net10.0, local installed game
and matching Harmony). The default is 1.7. Settings use Framework-compatible
`Math.Min/Max` and non-generic `Enum.IsDefined` on both targets.

`LoadFolders.xml` loads `Common` plus the corresponding `1.6`/`1.7` directory;
the mod assembly is present once per selected version. Shared quest XML and
languages live in `Common`. Root-level `Assemblies/` is excluded. Legacy
`ER_AncientComplex` XML lives only in `1.7/Defs` because it describes the earlier
1.7 layout system. Existing supported save field names are unchanged. Runtime
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
is assigned as active and starts the full 3,600,000-tick cooldown before being
registered with the quest manager and announced. Offer expiration also consumes
the opportunity. No failure path shortens the cooldown.

`Scribe_Values`, `Scribe_References` and normal QuestManager serialization own all
saved state. Loaded-game reconciliation clears an orphan active reference and
retains cooldown. No mutable static gameplay state or custom serializer.

`ResurrectionSettings : ModSettings` persists the arrival enum and minimum days
using vanilla mod preferences. `ResurrectionMod` exposes radio buttons and a
whole-day slider through the standard settings window. The static settings
reference is a user preference object, not world/save state. Changing settings
affects future deaths only; existing pending timestamps remain unchanged.

## One independent quest, four stages

`ER_Resurrection` has weight zero and is only launched by our delayed trigger.
`QuestNode_Resurrection : QuestNode` composes Core quest parts and four vanilla
`Outpost` sites. There is no Ideology activity check or Ideology quest root.
One hostile humanlike faction with a Settlement pawn-group maker defends all
four locations. No suitable faction/site means no opportunity is generated.

The sites are serialized up front; only the next site spawns after the previous
stage's saved signal and a one-day vanilla delay. Vanilla `GenStep_Outpost` and
`GenStep_Power` build and defend the maps. No custom map generator or terminal.
Securing an outpost narratively recovers shipment records; no new intel item.
Random outpost loot value is zero so the mission does not roll a second serum.
Defender equipment and ordinary generated furnishings remain vanilla.

Each lead uses `QuestPart_PassActivable` to forward the site's Core
`AllEnemiesDefeated` signal once. Quest tags use unique quest IDs. Leaving a site
before completion, site destruction, or its 30-day deadline fails the whole chain.
Completed stages disable those failure gates before later removal. No retry.

The base budget is vanilla current threat points (minimum 300) x 1.5, raised to
the chosen faction's vanilla minimum Settlement budget. Stage four uses 1.5x
that effective budget with the same faction. No hardcoded pawn counts or custom
combat logic. No peaceful variant is offered while violent quests are disabled.

The final `QuestPart_SerumRecovery` reacts to `AllEnemiesDefeated`, rechecks
current threats including dormant/fogged enemies and emits its result once with
a saved completion latch. Its old `terminalsHackedSignal`/`terminalsHacked` field
names are retained for save compatibility; new quests wire the records-recovered
event to the site's clearance signal. Previously generated quests retain their
old terminal signal wiring. No per-pawn or per-tick combat polling.

`ER_AncientComplex` remains as a legacy-only XML definition with
`MayRequire="Ludeon.RimWorld.Ideology"`. The engine skips it before XML inheritance
when Ideology is absent. New quests never use it. Complete older active quests
with Ideology enabled before removing that DLC; no migration is attempted.

## Reward and cleanup

Exactly one vanilla `MechSerumResurrector` thing is instantiated at generation.
`Reward_Items` displays it; `QuestPart_DropPods` deeply owns the same item until
successful recovery. The pod drops at the final site, not a possibly abandoned
home map. The drop's result signal ends the quest successfully; failure cleanup
destroys the undelivered serum. The completed recovery latch and empty delivered
inventory prevent duplicate rewards. The player must collect the landed pod
before leaving, as with ordinary site loot.

`QuestPart_ResurrectionCleanup` reports every cleaned-up outcome to the component
and removes/marks our spawned sites for removal using vanilla cleanup. The
component also reconciles historical/removed quests. Neither resets cooldown.
The quest UI's Dismiss button hides a quest; vanilla dismissal is not quest
failure. An ignored offer expires after ten days; an abandoned active site or
expired stage fails. The deadlines avoid an impossible chain reserving the slot
forever. Corpse loss or resurrection elsewhere never cancels the quest.

## Ideology isolation

No relic/precept reference is passed into our slate or persisted. No relic-info,
development points or relic history reward is added. The serializer stores vanilla
quest parts and our small objective/cleanup parts, never ideology state. Vanilla
serum use and all resurrection consequences remain unchanged.
