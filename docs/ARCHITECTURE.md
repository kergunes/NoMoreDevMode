# Architecture

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
`TimeAsColonistOrColonyAnimal` record must contain at least 1,200,000 ticks. This
is **cumulative actual membership**, not continuous membership or biological age;
former colonists retain legitimately earned membership time. Its worker excludes
prisoner, slave and extra home faction time. No separate membership ledger.

## Save state and scheduling

`ResurrectionState : GameComponent` is one instance per game, across all maps and
caravans. It scribes `nextEligibleTick`, `pendingTick`, `chainActive`, `activeQuest`,
and the triggering pawn's immutable narrative name/ID and death tick. No corpse
reference is required. The generated quest description owns its narrative text.

Eligible death -> one pending timestamp, randomly 60,000–240,000 ticks later.
Any death while pending, active or before `nextEligibleTick` is discarded immediately.
Every 250 ticks, one cheap component check handles the due event and reconciles a
missing/historical quest. It does not scan or tick pawns. No catch-up death queue.
If violent quests are disabled or no suitable ground site exists when due, the
opportunity is discarded without a cooldown; a future death is required.

The pending timestamp is cleared before generation. A successful generated quest
is assigned as active and starts the full 3,600,000-tick cooldown before being
registered with the quest manager and announced. Offer expiration also consumes
the opportunity. No failure path shortens the cooldown.

`Scribe_Values`, `Scribe_References` and normal QuestManager serialization own all
saved state. Loaded-game reconciliation clears an orphan active reference and
retains cooldown. No mutable static gameplay state or custom serializer.

## One independent quest, four stages

`ER_Resurrection` has weight zero and is only launched by our delayed trigger.
`QuestNode_Resurrection : QuestNode_Root_AncientComplex` composes vanilla quest
parts. It generates four serialized site sketches up front, but only spawns the
next site after the previous stage's signal and a one-day vanilla delay. All
four stages use the vanilla ancient complex generator, rooms, terminals and
threat curve. There are no new map generators or custom encounter archetypes.
`ER_AncientComplex` inherits vanilla's layout and excludes its security-crate
threat: that crate can independently roll resurrector serum. Ordinary room loot
uses the vanilla default supply table, which contains no resurrector serum.

Each stage's `QuestPart_PassAllActivable` persists individual terminal progress.
Quest tags use unique quest IDs. Unhacked terminal destruction passes through
`QuestPart_Filter_Hacked` into a gated failure. Leaving a site before completion,
site destruction, or its 30-day deadline fails the whole chain. Completed
stages disable those failure gates before later map removal. No retry/reroll.

Stage four has 1.5x the earlier stages' generation points and an additional
vanilla `SleepingMechanoids` site part. The base budget is vanilla current threat
points (minimum 300) x 1.5, frozen at opportunity generation. Vanilla faction
minimum point budgets apply; no hardcoded pawn counts. No peaceful variant is
offered while violent quests are disabled.

`QuestPart_SerumRecovery` remembers all-terminal completion and reacts to that
event or the site's vanilla `AllEnemiesDefeated` event. It checks current threats
with dormant and fogged enemies included. An earlier clearance cannot authorize
victory while threats remain. It emits its success signal once, with a saved
completion latch. No polling or custom combat logic.

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
