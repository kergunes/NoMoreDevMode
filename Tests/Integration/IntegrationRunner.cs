using System;
using System.IO;
using System.Linq;
using EarnedResurrection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace EarnedResurrectionIntegration;

// Separate opt-in mod, never shipped as part of the playable mod.
// Drives real game APIs and Scribe; forced ticks/hacks/kills are test fixtures,
// not evidence of natural gameplay difficulty or caravan UI acceptance.
public sealed class IntegrationRunner : GameComponent
{
    private int phase;
    private int expectedPending;
    private int expectedCooldown;
    private int expectedQuestId;
    private int frames;
    private bool running;

    public IntegrationRunner(Game game) { }
    private ResurrectionState State => Current.Game.GetComponent<ResurrectionState>();
    private static string ReportPath => Path.Combine(GenFilePaths.SaveDataFolderPath, "integration-results.txt");

    public override void GameComponentUpdate()
    {
        if (!GenCommandLine.CommandLineArgPassed("erIntegration") || running ||
            Current.ProgramState != ProgramState.Playing || Find.AnyPlayerHomeMap == null ||
            LongEventHandler.AnyEventNowOrWaiting || ++frames < 90)
            return;
        running = true;
        Find.TickManager.Pause();
        try
        {
            switch (phase)
            {
                case 0: BeforePendingSave(); break;
                case 1: AfterPendingLoad(); break;
                case 2: AfterActiveLoad(); break;
                case 3: AfterFailureLoad(); break;
                case 4: AfterFinalLoad(); break;
            }
        }
        catch (Exception ex)
        {
            File.AppendAllText(ReportPath, "FAIL phase " + phase + ": " + ex + "\n");
            Log.Error("[ER Tests] " + ex);
            phase = 99;
        }
    }

    private void BeforePendingSave()
    {
        File.WriteAllText(ReportPath, "RimWorld in-process integration checks, " + VersionControl.CurrentVersionString + "\n");
        Check(ModsConfig.IdeologyActive && !ModsConfig.RoyaltyActive && !ModsConfig.BiotechActive &&
            !ModsConfig.AnomalyActive && !ModsConfig.OdysseyActive, "Core + Ideology only, plus Harmony and test mod");
        Pawn established = MakeColonist(true);
        Check(ColonistDeathPatch.IsEligible(established), "established colonist eligible");
        Pawn recruit = MakeColonist(false);
        Check(!ColonistDeathPatch.IsEligible(recruit), "recent recruit excluded");
        established.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner);
        Check(!ColonistDeathPatch.IsEligible(established), "prisoner excluded");
        established.guest.SetGuestStatus(null);
        established.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Slave);
        Check(!ColonistDeathPatch.IsEligible(established), "slave excluded");
        established.guest.SetGuestStatus(null);
        if (established.Faction != Faction.OfPlayer) established.SetFaction(Faction.OfPlayer);
        established.guest.SetGuestStatus(Faction.OfPlayer);
        Check(!ColonistDeathPatch.IsEligible(established), "hosted guest excluded");
        established.guest.SetGuestStatus(null);
        Pawn animal = PawnGenerator.GeneratePawn(PawnKindDefOf.Muffalo, Faction.OfPlayer);
        Check(!ColonistDeathPatch.IsEligible(animal), "animal excluded");
        Pawn mech = PawnGenerator.GeneratePawn(PawnKindDefOf.Mech_Scyther, Faction.OfMechanoids);
        Check(!ColonistDeathPatch.IsEligible(mech), "mechanoid excluded");
        Check(State.PendingTick < 0, "negative eligibility cases did not schedule");
        established.Kill(null);
        expectedPending = State.PendingTick;
        Check(expectedPending >= Find.TickManager.TicksGame + 60000 &&
            expectedPending <= Find.TickManager.TicksGame + 240000, "real Pawn.Kill schedules 1–4 day delay");
        Check(State.NextEligibleTick == 0 && !State.ChainActive, "death itself starts no cooldown or chain");
        State.NotifyEligibleDeath(established);
        MakeColonist(true).Kill(null);
        Check(State.PendingTick == expectedPending, "duplicate notification / simultaneous death cannot replace pending death");
        established.Corpse?.Destroy();
        SaveReload(1, "ER_pending");
    }

    private void AfterPendingLoad()
    {
        Check(State.PendingTick == expectedPending, "A: pending trigger survives actual save/load despite corpse destruction");
        SetTick(expectedPending - 500);
        State.GameComponentTick();
        Check(State.ActiveQuest == null, "quest does not appear before scheduled delay");
        SetTick(expectedPending + 250);
        State.GameComponentTick();
        Quest quest = State.ActiveQuest;
        Check(quest != null && State.ChainActive && State.PendingTick < 0, "delayed generation creates exactly one active opportunity");
        expectedQuestId = quest.id;
        expectedCooldown = State.NextEligibleTick;
        Check(expectedCooldown == Find.TickManager.TicksGame + 3600000, "60-day cooldown starts at actual generation");
        MakeColonist(true).Kill(null);
        Check(State.PendingTick < 0 && State.ActiveQuest == quest, "death during active offer/cooldown ignored");
        quest.Accept(null);
        var sites = Sites(quest);
        Check(sites.Length == 4 && sites.Count(x => x.Spawned) == 1, "3 lead sites + final; only first spawned on acceptance");
        Check(!quest.PartsListForReading.Any(p => p is QuestPart_SubquestGenerator_RelicHunt), "no relic hunt generator");
        var rewards = quest.PartsListForReading.OfType<QuestPart_Choice>().SelectMany(x => x.choices).SelectMany(x => x.rewards).ToList();
        Check(rewards.Count == 1 && rewards[0] is Reward_Items, "only one explicit item reward; no relic info or ideology points");
        var items = ((Reward_Items)rewards[0]).items;
        Check(items.Count == 1 && items[0].def.defName == "MechSerumResurrector" && items[0].stackCount == 1 && !items[0].IsRelic(), "exactly one generic vanilla serum reward");
        Map map = MakeMap(sites[0]);
        Check(Terminals(map).Length > 0, "real vanilla complex generation spawned hackable terminal(s)");
        Terminals(map)[0].TryGetComp<CompHackable>().HackNow();
        SaveReload(2, "ER_active");
    }

    private void AfterActiveLoad()
    {
        Quest quest = State.ActiveQuest;
        Check(quest != null && quest.id == expectedQuestId && State.NextEligibleTick == expectedCooldown,
            "B/D: active quest reference, cooldown and no duplicate survive reload");
        for (int index = 0; index < 3; index++)
        {
            Site site = Sites(quest)[index];
            Map map = MakeMap(site);
            foreach (Thing terminal in Terminals(map)) terminal.TryGetComp<CompHackable>().HackNow();
            Check(!quest.Historical, "lead " + (index + 1) + " completes without ending chain");
            Advance(60000);
            quest.QuestTick();
            Check(Sites(quest)[index + 1].Spawned, "stage " + (index + 2) + " unlocks through saved delay/signals");
        }
        Site final = Sites(quest)[3];
        Map finalMap = MakeMap(final);
        Check(finalMap.mapPawns.AllPawnsSpawned.Any(p => p.RaceProps.IsMechanoid && p.Faction == Faction.OfMechanoids),
            "final actually spawns vanilla mechanoid defenders");
        foreach (Thing terminal in Terminals(finalMap)) terminal.TryGetComp<CompHackable>().HackNow();
        Check(!quest.Historical, "hacking final terminals alone cannot grant serum while defenders remain");
        Check(!finalMap.listerThings.AllThings.Any(x => x.def.defName == "MechSerumResurrector"), "no serum spawned before final victory");
        // Fail this first chain; a later chain will exercise actual reward delivery.
        quest.End(QuestEndOutcome.Fail, sendLetter: false);
        Check(!State.ChainActive && State.NextEligibleTick == expectedCooldown, "failure clears active flag without cooldown refund");
        SaveReload(3, "ER_failed");
    }

    private void AfterFailureLoad()
    {
        Check(!State.ChainActive && State.PendingTick < 0 && State.NextEligibleTick == expectedCooldown,
            "C/D: failure and cooldown persist through reload");
        MakeColonist(true).Kill(null);
        Check(State.PendingTick < 0, "death during remaining cooldown is not queued");
        SetTick(expectedCooldown + 250);
        State.GameComponentTick();
        Check(State.PendingTick < 0 && !State.ChainActive, "E: expiry does not resurrect old death queue");
        MakeColonist(true).Kill(null);
        Check(State.PendingTick >= 0, "new post-cooldown death schedules a new opportunity");
        SetTick(State.PendingTick + 250);
        State.GameComponentTick();
        Quest quest = State.ActiveQuest;
        quest.Accept(null);
        expectedCooldown = State.NextEligibleTick;
        for (int index = 0; index < 3; index++)
        {
            Map map = MakeMap(Sites(quest)[index]);
            foreach (Thing terminal in Terminals(map)) terminal.TryGetComp<CompHackable>().HackNow();
            Advance(60000);
            quest.QuestTick();
        }
        Site final = Sites(quest)[3];
        Map finalMap = MakeMap(final);
        foreach (Thing terminal in Terminals(finalMap)) terminal.TryGetComp<CompHackable>().HackNow();
        foreach (Pawn pawn in finalMap.mapPawns.AllPawnsSpawned.ToList())
            if (pawn.Faction != Faction.OfPlayer && (pawn.RaceProps.IsMechanoid || pawn.RaceProps.Insect || pawn.HostileTo(Faction.OfPlayer)))
                pawn.Kill(null);
        // Some complex threats are buildings, not pawns. Remove only hostile attack targets.
        foreach (Thing thing in finalMap.listerThings.AllThings.ToList())
            if (thing is Building && thing.HostileTo(Faction.OfPlayer)) thing.Destroy();
        Check(!GenHostility.AnyHostileActiveThreatToPlayer(finalMap, true, true), "all final threats cleared in test fixture");
        // Execute the real site's clearance detector without waiting for its interval.
        AccessTools.Method(typeof(Site), "CheckAllEnemiesDefeated").Invoke(final, null);
        Check(quest.State == QuestState.EndedSuccess && !State.ChainActive, "final clearance -> vanilla pod delivery -> quest success");
        var drop = quest.PartsListForReading.OfType<QuestPart_DropPods>().Single();
        Check(!drop.Things.Any(), "delivered serum removed from pending pod inventory");
        QuestUtility.SendQuestTargetSignals(final.questTags, "AllEnemiesDefeated", final.Named("SUBJECT"));
        Check(!State.ChainActive && State.NextEligibleTick == expectedCooldown, "duplicate final signal cannot restart chain or refund cooldown");
        expectedQuestId = quest.id;
        SaveReload(4, "ER_success");
    }

    private void AfterFinalLoad()
    {
        Check(!State.ChainActive && State.PendingTick < 0 && State.NextEligibleTick == expectedCooldown,
            "successful completion remains closed after reload");
        Quest quest = Find.QuestManager.QuestsListForReading.Single(q => q.id == expectedQuestId);
        Check(quest.State == QuestState.EndedSuccess, "successful quest state survives reload");
        File.AppendAllText(ReportPath, "AUTOMATED IN-GAME INTEGRATION COMPLETE. Human caravan/combat/balance acceptance remains pending.\n");
        Log.Message("[ER Tests] Integration complete: " + ReportPath);
        phase = 99;
    }

    private Pawn MakeColonist(bool established)
    {
        Pawn pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(Find.AnyPlayerHomeMap.Center, Find.AnyPlayerHomeMap, 15), Find.AnyPlayerHomeMap);
        if (established)
            ((DefMap<RecordDef, float>)AccessTools.Field(typeof(Pawn_RecordsTracker), "records").GetValue(pawn.records))
                [RecordDefOf.TimeAsColonistOrColonyAnimal] = 1200000f;
        return pawn;
    }

    private static Site[] Sites(Quest quest) => quest.PartsListForReading.OfType<QuestPart_SpawnWorldObject>().Select(p => (Site)p.worldObject).ToArray();
    private static Thing[] Terminals(Map map) => map.listerThings.AllThings.Where(t => t.def == ThingDefOf.AncientTerminal).ToArray();
    private static Map MakeMap(Site site)
    {
        Check(site.Spawned, "site spawned before map generation");
        Map map = GetOrGenerateMapUtility.GetOrGenerateMap(site.Tile, new IntVec3(250, 1, 250), null);
        map.fogGrid.ClearAllFog();
        return map;
    }
    private static void Check(bool condition, string label)
    {
        File.AppendAllText(ReportPath, (condition ? "PASS " : "FAIL ") + label + "\n");
        if (!condition) throw new InvalidOperationException(label);
    }
    private static void SetTick(int tick) => Find.TickManager.DebugSetTicksGame(((tick + 249) / 250) * 250);
    private static void Advance(int ticks) => SetTick(Find.TickManager.TicksGame + ticks);
    private void SaveReload(int nextPhase, string file)
    {
        phase = nextPhase;
        GameDataSaveLoader.SaveGame(file);
        Check(File.Exists(GenFilePaths.FilePathForSavedGame(file)), "saved " + file);
        GameDataSaveLoader.LoadGame(file);
    }
    public override void ExposeData()
    {
        Scribe_Values.Look(ref phase, "phase");
        Scribe_Values.Look(ref expectedPending, "expectedPending");
        Scribe_Values.Look(ref expectedCooldown, "expectedCooldown");
        Scribe_Values.Look(ref expectedQuestId, "expectedQuestId");
    }
}
