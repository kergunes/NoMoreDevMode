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
- With any DLC pool enabled, eligible unused DLC roots must be chosen before Core.
  Native operations appear as linked subquests; accept them if required. Completing
  one must open the parent progress dialog, then respect the configured interval.
- In settings, only active DLC have pool checkboxes: Royalty, Ideology, Biotech,
  Odyssey, all enabled by default. Anomaly shows an exclusion note, with no toggle.
  Toggle each pool off, generate a NEW chain, and confirm that DLC is not selected.
  Disable all pools to get Core leads and a Core final. Restart to check persistence.
  Expand each DLC and disable one encounter: a new chain must exclude only that
  encounter, leaving its DLC's other encounters eligible. Individual checkboxes
  start enabled, survive restart, and are preserved across whole-pool toggles.
  Expand every DLC list and scroll to the final setting, then collapse them and
  scroll back up. Repeat in a smaller window: no rows may disappear into a hidden
  second column, and the page must not collapse to its first label/radio button.
  Changing preferences during an existing chain must not change its saved pool.
- Exercise each enabled pool separately, using repeated new chains: Royalty five
  types, Ideology five types, Odyssey five types. Biotech must show an empty-pool
  note without encounter controls. Native
  prerequisites can make some unavailable.
- Mechanitor's tomb, descending signal and grounded wreck must be absent from
  settings and new lead selection, including future stages in an older saved
  parent chain. An already generated child must remain usable and unchanged.
- The bloodbound transport and The awakened company must also be absent from
  settings and future lead selection, including old saved parent pools.
- Independent shrine/drone/archive leads need terminal hacking AND clearance.
  The distant native complex mission also needs Royalty and a player relic for
  setup; its information reward must not advance a real relic hunt.
- Ground opportunity sites require opening the sealed shipment archive after
  clearing defenders; just arriving, claiming an engine or killing one leader
  must not advance the chain. Destroying that archive must fail it.
- For legacy sanguophage crashes only, keep enemies alive past the native timer:
  the chain must wait for clearance. Hospitality must
  use human attackers and wait for the guests' successful departure/operation end.
- Never select a mech-cluster quest, mechanitor ship/boss, Anomaly, gravcore or
  orbital mission. Problem-causer must use the pirate branch, never the cluster.
- With no eligible enabled DLC leads, expect distinct types from: occupied depot, sleeping mechanoid
  yard, automated turret relay, manhunter waystation, ambushed rendezvous. Clear
  threats to recover each lead. No hacking dependency or Ideology content may load.
- No encounter type repeats within one chain, including after save/reload.
- Slider 1–5 selects LEADS, default 3; a separate final always follows. Verify
  both endpoints and the default. Changing it during a chain must not alter that
  already generated plan. New chains use the new setting.

## Physical reward isolation (newly generated DLC leads)

- Run each scenario both as a shipment child and as the ordinary vanilla quest;
  vanilla must retain its original rewards, research, ownership and completion.
- Generate the five removed roots as ordinary VANILLA quests: mechanitor complex,
  mechanoid signal, gravship wreckage, sanguophage ship, ancient mercenaries. They
  must still exist in vanilla. Check mechlink extraction, native motor inspection/
  research/claim/launch, sanguophage genes and unique weapon loot as applicable.
  No shipment warning, damaged-engine message or shipment completion may appear.
- Already saved removed ER children retain saved quest parts, but their old
  engine/pawn/weapon Harmony restrictions no longer exist. Previously removed
  saved items/genes/implants are not recreated; use freshly generated vanilla
  quests for the normal-behavior comparison.
- On a normal vanilla site with the same quest root as a current allowed child,
  crates must retain native special loot and opening/trap behavior. Repeat on
  our own child: special loot filtering must apply only there. A stale site tag
  without matching current child/parent/site ownership must not enable filtering.
- Odyssey crates / native distant complex: opening crates/security crates must
  retain their triggers and traps, ordinary salvage and guards, with no bonus
  serum, unique weapon or special progression item. Minified packages must never
  be left empty. Save/reload before opening and after enemy downing to check markers.
- Bring player colonists, a serum, special gear and minified upgrades into a lead
  site: their existing items/implants/genes must remain untouched. Test the separate
  final cache and recover its one serum normally.
- Existing old leads and previously acquired items must remain unchanged; generate
  new children for this revision. These checks are NOT claimed passed by compilation.

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
  Enabled Ideology uses a defended ancient complex; fallback uses a defended outpost.
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
  Fail/expire an active DLC subquest: the parent must fail and retain cooldown.
  Save/load during its offer, active combat, archive opening and post-completion
  wait: there must be no duplicated child, repeated type, progress popup or reward.
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
