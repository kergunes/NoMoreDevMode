# Human verification checklist

## Evidence and scope

Current varied-encounter/cache version: production builds target RimWorld 1.6
(net472, RimRef 1.6.4871 + matching Harmony) and 1.7 (installed 1.7.4984 +
Harmony). Compilation does not prove XML loading, map generation or gameplay.
No game or automated gameplay suite was run for this revision; the user owns
in-game testing. HUMAN VERIFICATION REQUIRED.

The old isolated startup attempt is historical only: it stopped before scenario
execution on a DLC fixture assertion. It supplies no acceptance for this version.
The opt-in 1.7 Tests/Integration harness source is partly adapted to the new Core
flow but has not been built/run here; it does not cover Ideology or every variant.
Do not enable it in a personal save.

## Prepare

- Restart after updating the DLL. Use a new/copied test save, never downgrade a
  1.7 save to test 1.6. Keep Harmony before Core and the mod.
- Run the checklist separately with Ideology and without it, on each game version.
  Leave hostile human factions and mechanoids present. If too few valid encounter
  types/factions exist, the mod cannot create the requested non-repeating chain.
- Check the log for missing defs, parent inheritance, texture, Harmony or Scribe
  errors. No proprietary DLL should be copied into the mod package.
- For a quick fresh test, choose Immediate arrival, 0 minimum membership days,
  0 days between leads and default 3 leads. Kill a free player colonist and unpause.
  Only one opportunity should appear. Save A before arrival on a delayed run.

## Names, variety and main paths

- Task/site titles must be descriptive and contain no stage numbers.
- With Ideology and 3 leads, expect an ancient complex, a guarded shrine terminal
  and a defended spacedrone encounter, in a combat-weighted order. No player relic
  is needed or selected. The shrine/drone are expedition adaptations, not the
  original relic quests. Hack their terminal(s) AND clear their guards.
- With Ideology and 4–5 leads, additional distinct Core encounters extend the chain.
- Without Ideology, expect distinct types from: occupied depot, sleeping mechanoid
  yard, automated turret relay, manhunter waystation, ambushed rendezvous. Clear
  threats to recover each lead. No hacking dependency or Ideology content may load.
- No encounter type repeats within one chain, including after save/reload.
- Slider 1–5 selects LEADS, default 3; a separate final always follows. Verify
  both endpoints and the default. Changing it during a chain must not alter that
  already generated plan. New chains use the new setting.

## Progress dialog and waiting

- After every lead, a direct message window should open explaining the recovered
  records and the next lead. It must not be only a transient toast/letter.
- The quest remains active while waiting. The waiting paragraph and native
  countdown should remain until the next location is revealed.
- Check 0 and a nonzero interval (0–7 days, default 1). Zero text must not tell the
  player to wait. Save B during a lead or interval; quit/load: same progress,
  no duplicate dialog, site, quest or renewed delay.
- Location discovery can still have a normal letter; that is separate from the
  direct progress message after completing the preceding lead.

## Mandatory final and physical reward

- Both paths, at every lead-count setting, must end with a separate final.
  Ideology uses a defended ancient complex; fallback uses a defended outpost.
- Find the sealed medical cache on the map. Inspect it: the single serum is held
  in the cache, not present as loose loot or an incoming pod.
- Opening while enemies/turrets/dormant or fogged threats remain must be blocked.
- Defeat the guards: the quest must STILL be active. Select a colonist and order
  the ordinary Open interaction on the case. The opening job must be reachable.
- Only after opening, exactly one vanilla resurrector serum should appear nearby
  and the quest should succeed. No reward pod should be created.
- Save before opening; quit/load and repeat. Reopening an empty case and reloading
  after success must not duplicate the serum.
- In a separate failure save, destroy the case: no serum ejection, chain fails.
  Expiring/failing the quest before opening must destroy its undelivered contents.
- Collect the serum before leaving. Vanilla resurrection restrictions and side
  effects remain unchanged. Corpse loss or prior resurrection does not break the chain.

## Failure, eligibility and global state

- Cooldown slider: whole days 0–120, default 60. Check both endpoints on new
  chains. At 0, deaths while pending/active still cannot create a second chain;
  only a new eligible death after completion can schedule another. Changing the
  slider must not shorten/extend an already stored cooldown. Restore 60 for the
  baseline cooldown acceptance below. Check the preference survives restart.

- Destroy an unhacked terminal: fail. Destroying an already hacked terminal after
  its encounter completes must not retroactively fail a later encounter.
- Abandon an unfinished site / remove its map / expire its 30-day deadline: fail.
  Ignore the offer for ten days: expire. Vanilla Dismiss merely hides the quest.
- Failure clears active state without refunding cooldown. Save C/D after failure
  and during cooldown; quit/load: cooldown must remain.
- Deaths while pending/active/cooling down are discarded, not queued.
- At cooldown expiry, nothing happens without a NEW eligible death (case E).
- Check the 20-day default membership threshold using vanilla Records. The slider
  supports 0–60 days. Prisoners, slaves (Ideology only), guests, animals, mechanoids
  and unrelated faction members never qualify. Caravan deaths should still work.
- Verify no changes to relic objectives, precepts or progress on the Ideology path.
- Settings survive restart. Old active quests retain old parts, names and reward
  behavior. Use a newly generated chain for these changes; do not remove Ideology
  from a save containing its active content.

## Debugging

Dev Mode → Earned Resurrection → Print resurrection state logs active/pending
and cooldown ticks. The log also records the chosen encounter plan for new quests.
Clear cooldown is for separate repeat runs, not real cooldown acceptance.
