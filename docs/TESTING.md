# Testing / human acceptance

## Actual evidence, 2026-10-21

| Check | Status |
| --- | --- |
| Local version/runtime and vanilla source/defs research | Inspected: 1.7.4984 rev1242, Waylith, .NET 10 |
| Main project build against installed game/Harmony | PASS: zero warnings/errors |
| Separate integration test assembly build | PASS: zero warnings/errors |
| Game startup / mod constructor / Harmony initialization | Reached in isolated test profile; no mod XML/patch error observed before fixture stopped |
| Core + Ideology only launch | NOT VERIFIED: fresh profile auto-enabled other owned DLC |
| Automated integration scenarios | STOPPED at initial fixture assertion, before any scenario ran |
| Natural gameplay, stage progression, actual reward, save/load | HUMAN VERIFICATION REQUIRED |

The user requested to do testing personally. The agent stopped its owned game
process and performed no subsequent gameplay tests. No personal save or active
mod configuration was changed. Raw interrupted-run evidence remains in ignored
`artifacts/integration-01/`, including `stdout.txt` and `integration-results.txt`.
Waylith wrote its runtime output to redirected stdout rather than the requested
Unity-style Player.log path. Unrelated installed-mod metadata warnings may appear
during discovery even when those mods are not enabled.

## Preparation

1. Make a **copy** of a colony save. Use an established free colonist whose Records
   show at least 20 days as a colonist. Biological age or 20 days of world age is
   insufficient. Recently recruited/enslaved pawns must not qualify.
2. In the mod list enable Harmony, Core, Ideology and **NoMoreDevMode / Earned
   Resurrection**. Disable the development-only integration-test mod. For the
   first acceptance run, disable unrelated mods and other DLC.
3. Restart the game and load the copied save. Check the log for
   `[EarnedResurrection] NoMoreDevMode / Earned Resurrection loaded.` and absence
   of XML/def/Harmony exceptions. Ordinary serum effects remain vanilla.
4. Dev Mode is optional for killing a test pawn or advancing time. Use the
   `Earned Resurrection` debug category's `Print resurrection state` action for
   pending/active/cooldown timestamps. `Clear resurrection cooldown` is for
   separate repeat runs; do not use it during the cooldown acceptance checks.

## Main gameplay run

1. Kill the established colonist. Expect no immediate quest. Print state: pending
   is set, active is false, cooldown has not started. Kill a second established
   colonist while pending: the first opportunity must remain the only one.
2. **Save A**, quit/load, and allow 1–4 days from the original death. One offer
   should name the first dead pawn and warn about preserving remains. The offer
   must contain exactly one serum reward and no relic-info reward.
3. Print state: active true, pending -1, cooldown end 60 days after generation.
   Accept. One first-lead site appears. Keep the body frozen if you intend to
   resurrect this pawn; loss of the body should not break the chain.
4. Send a real caravan to the marked complex. Hack every marked ancient terminal.
   One day later the second site appears; repeat for leads two and three.
   **Save B** with a partially completed stage, quit/load, and verify the same
   quest and terminal progress resume without a second chain.
5. The fourth site must include sleeping mechanoid defenders and be harder than
   a lead. Hack all terminals while enemies remain: there must be no serum yet.
   Clear all threats, including dormant/fogged enemies. Expect one reward pod
   at this site and quest success. Open/collect it: **exactly one resurrector
   mech serum**, no relic. Repeated signals/reload must not produce another.
6. Save/load after success. Active false, cooldown unchanged. Vanilla serum use
   should behave normally; no new resurrection rules or item exist.

## Failure and cooldown runs (separate copied saves)

- Destroy an unhacked terminal: chain fails, no reward, active clears, cooldown stays.
- Destroy a terminal already hacked after the stage is complete: no retroactive failure.
- Leave a site before completing it / allow its map to be removed: chain fails.
- Let a site's 30-day deadline expire: chain fails, including the final stage.
- Ignore an offer for ten days: offer expires and releases the active slot; cooldown stays.
- Dismiss only hides the vanilla quest UI. It does not itself mean failure; use
  site abandonment, deadline or failed objectives to test failed opportunities.
- **Save C/D** after failure and during cooldown, quit/load: no cooldown reset.
- Kill established pawns during active chain and after failure but before cooldown
  expiry: no extra pending trigger or chain.
- **Case E:** reach cooldown end without a new death. Nothing appears. Only a
  **new** eligible death schedules the next 1–4 day opportunity.

## Eligibility and edge cases

Test separately: recent recruit, prisoner, slave, guest/quest lodger, visitor,
hostile/neutral human, animal, mechanoid and off-map faction member: no trigger.
An established player caravan colonist death should trigger just like a map death.
Check multiple home maps, caravan travel, relocation, pending saves without a
corpse and a pawn resurrected elsewhere mid-chain. Narrative remains associated
with the original death; quest continues and reward remains generic.

Before/after the chain compare actual ideology relic objectives, precepts and
relic progress: all unchanged, including an ideology with no relic configured.

## Optional automated harness (not run further)

`Tests/Integration` is a separate developer-only mod and project, excluded from
the main solution. It exercises real game APIs with forced ticks, generated
maps, forced hacking/deaths and real Scribe save/load. These fixtures cannot
prove natural combat, balance or caravan UI acceptance.

If explicitly resuming automated tests later, build both projects with the same
game/Harmony properties. Enable the test mod only in a dedicated
`-savedatafolder=...` profile and launch `-quicktest -erIntegration`. Populate
`knownExpansions` with **all locally owned DLC IDs**, while `activeMods` includes
only Core, Ideology, Harmony and the two local mods; otherwise the game adds
newly discovered owned expansions and the initial assertion fails. The harness
writes `integration-results.txt` to that dedicated folder. Do not enable it in a
personal save. No automated integration PASS is claimed for this revision.
