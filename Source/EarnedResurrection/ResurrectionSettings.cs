using System;
using Verse;

namespace EarnedResurrection;

public enum OpportunityDelay
{
    Immediate,
    OneToTwoDays,
    WithinAWeek
}

public sealed class ResurrectionSettings : ModSettings
{
    public OpportunityDelay delay = OpportunityDelay.OneToTwoDays;
    public int minimumMembershipDays = ResurrectionTuning.DefaultMinimumMembershipDays;
    public int betweenStagesDays = ResurrectionTuning.DefaultBetweenStagesDays;
    public int leadCount = 3;
    public int cooldownDays = ResurrectionTuning.DefaultCooldownDays;

    public int RollDelayTicks()
    {
        switch (delay)
        {
            case OpportunityDelay.Immediate:
                return 0;
            case OpportunityDelay.WithinAWeek:
                return Rand.RangeInclusive(ResurrectionTuning.TicksPerDay, 7 * ResurrectionTuning.TicksPerDay);
            default:
                return Rand.RangeInclusive(ResurrectionTuning.TicksPerDay, 2 * ResurrectionTuning.TicksPerDay);
        }
    }

    public void ClampValues()
    {
        minimumMembershipDays = Math.Max(0, Math.Min(60, minimumMembershipDays));
        betweenStagesDays = Math.Max(0, Math.Min(ResurrectionTuning.MaximumBetweenStagesDays, betweenStagesDays));
        leadCount = Math.Max(1, Math.Min(5, leadCount));
        cooldownDays = Math.Max(0, Math.Min(ResurrectionTuning.MaximumCooldownDays, cooldownDays));
        if (!Enum.IsDefined(typeof(OpportunityDelay), delay))
            delay = OpportunityDelay.OneToTwoDays;
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref delay, "opportunityDelay", OpportunityDelay.OneToTwoDays);
        Scribe_Values.Look(ref minimumMembershipDays, "minimumMembershipDays", ResurrectionTuning.DefaultMinimumMembershipDays);
        Scribe_Values.Look(ref betweenStagesDays, "betweenStagesDays", ResurrectionTuning.DefaultBetweenStagesDays);
        Scribe_Values.Look(ref leadCount, "leadCount", 3);
        Scribe_Values.Look(ref cooldownDays, "cooldownDays", ResurrectionTuning.DefaultCooldownDays);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
            ClampValues();
    }
}
