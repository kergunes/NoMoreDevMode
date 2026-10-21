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
        minimumMembershipDays = Math.Clamp(minimumMembershipDays, 0, 60);
        if (!Enum.IsDefined(delay))
            delay = OpportunityDelay.OneToTwoDays;
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref delay, "opportunityDelay", OpportunityDelay.OneToTwoDays);
        Scribe_Values.Look(ref minimumMembershipDays, "minimumMembershipDays", ResurrectionTuning.DefaultMinimumMembershipDays);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
            ClampValues();
    }
}
