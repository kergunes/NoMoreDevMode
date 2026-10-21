# Testing / human acceptance

## Current status — 2026-10-22

The DLC-independent production DLL compiled successfully against installed
RimWorld 1.7.4984 rev1242 and Harmony, with zero warnings/errors. No game was
launched and no automated gameplay tests were run for this revision, respecting
the user's request to test personally. **HUMAN VERIFICATION REQUIRED.**

Historical evidence: the older Ideology-dependent version reached startup in an
isolated profile. Its automated attempt stopped at the initial DLC fixture
assertion because the game auto-enabled other owned expansions. That run proves
neither Core-only loading nor quest progression. Historical output remains in
ignored `artifacts/integration-01/`.

## Preparation

1. Use a new test world with **Harmony → Core → NoMoreDevMode / Earned
   Resurrection** only. Disable all DLC and the separate integration-test mod.
   Leave a hostile pirate/tribal faction enabled at world creation; the quest
   needs a hostile humanlike faction capable of defending a Core outpost.
2. Do not disable Ideology on a valuable existing save merely for this test.
   Previously generated quests still contain DLC complexes and terminals.
   Finish those with Ideology enabled before removing the DLC. New quests use
   Core outposts regardless of whether Ideology is installed.
3. Restart after updating the DLL. Check startup for XML/def/Harmony errors.
   The log should contain `[EarnedResurrection] ... loaded.`.
4. For a quick test, open Options → Mod settings → NoMoreDevMode / Earned
   Resurrection. Set **Immediately** and **0 minimum membership days**. Keep
   violent quests enabled. Existing pending/active quests and cooldown still apply.

## Main gameplay run

1. Kill a free player colonist and unpause. On the next game tick, one opportunity
   should name the dead pawn and show exactly one resurrector serum reward.
   Preserve the body if you plan to use the serum. Corpse loss must not crash or
   cancel the quest.
2. Accept. Only the first outpost should appear. Send a real caravan, attack it,
   and defeat its defenders. There are no terminal objectives in new quests.
   Clearing the outpost narratively recovers shipment records.
3. One day later, the next outpost appears. Repeat for three leads. Old completed
   sites disappearing must not retroactively fail the chain.
4. The fourth outpost should have the same enemy faction and a higher threat
   budget (1.5x the leads). Entering without defeating its defenders must award
   nothing. Clear all threats, including turrets and any dormant/fogged enemies.
5. Expect quest success and exactly **one vanilla resurrector mech serum** in a
   reward pod at the final site. Collect it before leaving. Reloading or repeated
   clearance must not create another serum. Resurrection behavior stays vanilla.

## Settings and eligibility

- Arrival options: Immediately (next unpaused tick), 1–2 days (default), Within
  one week (1–7 days). Try each in a separate state without an active/pending
  opportunity or cooldown. Changing options must not reschedule a pending death.
- Minimum membership: whole-day slider 0–60, default 20. Uses actual cumulative
  free-colonist membership from vanilla Records, not biological/world age.
- Reset preferences to 20 days and 1–2 days for normal baseline testing.
- With 20 selected, recent recruits must not qualify. Prisoners, visitors, quest
  guests, animals, mechanoids and off-map faction members never qualify. Test
  slaves in a separate optional-Ideology run; Core has no slave feature.
- Established caravan colonist deaths should work like map deaths. Check multiple
  maps, caravan travel and relocation. Mental breaks/downed status alone do not
  disqualify an established free colonist.
- Settings should survive closing the dialog and restarting the game.

## Persistence, failure and cooldown

- **A:** choose delayed arrival, kill an eligible pawn, save/quit/load before the
  due time. The same one opportunity should appear on schedule.
- **B:** save/quit/load during an active expedition. Same quest, sites and stage;
  no duplicate chain. Repeat during the one-day gap between leads.
- Leave an unfinished site, allow its map to be removed, or let its 30-day
  deadline expire: fail the chain, no serum, active state clears.
- Ignore the offer for ten days: slot clears, cooldown remains. Vanilla Dismiss
  only hides the quest; it is not abandonment.
- **C/D:** after failure and during cooldown, save/quit/load: cooldown remains.
  The 60-day cooldown starts at generation, not death/acceptance.
- Kill other eligible pawns while pending, active or in cooldown: no extra queue.
- **E:** after cooldown expiry, no old death generates a quest. A **new** death
  is required.
- Destroy the corpse or resurrect the named pawn elsewhere mid-chain: chain
  continues, reward remains generic.
- Optional Ideology run: relic/precept/progress state must remain unchanged.

## Debug tools and optional harness

Dev Mode → Earned Resurrection → Print resurrection state logs pending tick,
active quest and cooldown end. Clear resurrection cooldown is for separate repeat
runs; do not use it while accepting the real cooldown behavior.

`Tests/Integration` is an opt-in developer-only mod, outside the main solution.
Its source has been adapted to Core outposts but was not built/run for this
revision. Forced ticks, maps, deaths and Scribe reloads would still be automation
evidence, not human combat/caravan/balance acceptance.

Only when explicitly resuming automation, build both projects with local game
and Harmony properties. Enable Core, Harmony and both local mods in a dedicated
`-savedatafolder=...` profile. Populate `knownExpansions` with **all owned DLC IDs**
but omit them from `activeMods` to prevent automatic activation. Launch with
`-quicktest -erIntegration`; output is `integration-results.txt` in that profile.
Never enable this test mod in a personal save. No integration PASS is claimed.
