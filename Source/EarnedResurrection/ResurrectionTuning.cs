namespace EarnedResurrection;

// Centralized V0.1 defaults; no settings UI until the loop is playtested.
public static class ResurrectionTuning
{
    public const bool Enabled = true;
    public const int TicksPerDay = 60000;
    public const int CooldownTicks = 60 * TicksPerDay;
    public const int MinimumMembershipTicks = 20 * TicksPerDay;
    public const float ThreatMultiplier = 1.5f;
    public const float FinalThreatMultiplier = 1.5f;
    public const int StageDeadlineTicks = 30 * TicksPerDay;
    public const int BetweenStagesTicks = TicksPerDay;
    public const int CheckInterval = 250;
}
