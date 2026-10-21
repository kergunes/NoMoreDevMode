using System;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace EarnedResurrection;

public sealed class ResurrectionState : GameComponent
{
    private int nextEligibleTick;
    private int pendingTick = -1;
    private string pawnName;
    private string pawnId;
    private int deathTick = -1;
    private bool chainActive;
    private Quest activeQuest;

    public ResurrectionState(Game game) { }

    public int NextEligibleTick => nextEligibleTick;
    public int PendingTick => pendingTick;
    public bool ChainActive => chainActive;
    public Quest ActiveQuest => activeQuest;

    public void NotifyEligibleDeath(Pawn pawn)
    {
        if (!ResurrectionTuning.Enabled || !ModsConfig.IdeologyActive ||
            !Find.Storyteller.difficulty.allowViolentQuests)
            return;

        ReconcileActiveQuest();
        int now = Find.TickManager.TicksGame;
        if (chainActive || pendingTick >= 0 || now < nextEligibleTick)
        {
            Log.Message("[EarnedResurrection] Ignoring death: pending opportunity, active chain or cooldown.");
            return;
        }
        pawnName = pawn.LabelShortCap;
        pawnId = pawn.GetUniqueLoadID();
        deathTick = now;
        pendingTick = now + Rand.RangeInclusive(ResurrectionTuning.TicksPerDay, 4 * ResurrectionTuning.TicksPerDay);
        Log.Message($"[EarnedResurrection] Eligible death: {pawnName}. Opportunity scheduled for tick {pendingTick}.");
    }

    public override void GameComponentTick()
    {
        if (Find.TickManager.TicksGame % ResurrectionTuning.CheckInterval != 0)
            return;
        ReconcileActiveQuest();
        if (pendingTick < 0 || Find.TickManager.TicksGame < pendingTick || chainActive)
            return;

        // No permanent delay queue if the feature becomes unavailable.
        if (!ResurrectionTuning.Enabled || !ModsConfig.IdeologyActive ||
            !Find.Storyteller.difficulty.allowViolentQuests)
        {
            ClearPending();
            return;
        }
        GenerateOpportunity();
    }

    private void GenerateOpportunity()
    {
        var def = DefDatabase<QuestScriptDef>.GetNamed("ER_Resurrection");
        var slate = new Slate();
        slate.Set("pawnName", pawnName);
        slate.Set("points", StorytellerUtility.DefaultThreatPointsNow(
            (IIncidentTarget)Find.AnyPlayerHomeMap ?? Find.World));
        if (!def.CanRun(slate, (IIncidentTarget)Find.AnyPlayerHomeMap ?? Find.World))
        {
            Log.Warning("[EarnedResurrection] Opportunity unavailable (no suitable expedition site/defenders). Waiting for a new death.");
            ClearPending();
            return;
        }

        // Consume the pending event before generation. A failed generator never loops
        // indefinitely, and a saved generated quest never retains a pending trigger.
        pendingTick = -1;
        Quest generated = QuestGen.Generate(def, slate);
        if (generated == null)
        {
            Log.Error("[EarnedResurrection] Quest generation failed; no cooldown started.");
            ClearPending();
            return;
        }
        activeQuest = generated;
        chainActive = true;
        nextEligibleTick = checked(Find.TickManager.TicksGame + ResurrectionTuning.CooldownTicks);
        Find.QuestManager.Add(generated);
        QuestUtility.SendLetterQuestAvailable(generated);
        Log.Message($"[EarnedResurrection] Questline generated for {pawnName}; global cooldown until tick {nextEligibleTick}.");
    }

    public void NotifyQuestEnded(Quest quest)
    {
        if (quest != activeQuest)
            return;
        Log.Message($"[EarnedResurrection] Resurrection quest ended: {quest.State}. Cooldown retained.");
        activeQuest = null;
        chainActive = false;
        ClearPending();
    }

    private void ReconcileActiveQuest()
    {
        // Also handles removal/offer expiration even if no custom callback executes.
        if (chainActive && (activeQuest == null || activeQuest.Historical || !Find.QuestManager.Contains(activeQuest)))
        {
            Log.Message("[EarnedResurrection] Cleared inactive chain; cooldown retained.");
            chainActive = false;
            activeQuest = null;
            ClearPending();
        }
    }

    private void ClearPending()
    {
        pendingTick = -1;
        pawnName = null;
        pawnId = null;
        deathTick = -1;
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref nextEligibleTick, "nextEligibleTick");
        Scribe_Values.Look(ref pendingTick, "pendingTick", -1);
        Scribe_Values.Look(ref chainActive, "chainActive");
        Scribe_References.Look(ref activeQuest, "activeQuest");
        Scribe_Values.Look(ref pawnName, "pawnName");
        Scribe_Values.Look(ref pawnId, "pawnId");
        Scribe_Values.Look(ref deathTick, "deathTick", -1);
    }

    public override void LoadedGame() => ReconcileActiveQuest();

    public void DebugClearCooldown()
    {
        if (Prefs.DevMode) nextEligibleTick = 0;
    }
}
