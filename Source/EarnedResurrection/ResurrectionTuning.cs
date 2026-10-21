namespace EarnedResurrection;

// Gameplay defaults; player-facing options are stored in ModSettings.
public static class ResurrectionTuning
{
    public const bool Enabled = true;
    public const int TicksPerDay = 60000;
    public const int CooldownTicks = 60 * TicksPerDay;
    public const int DefaultMinimumMembershipDays = 20;
    public const float ThreatMultiplier = 1.5f;
    public const float FinalThreatMultiplier = 1.5f;
    public const int StageDeadlineTicks = 30 * TicksPerDay;
    public const int DefaultBetweenStagesDays = 1;
    public const int MaximumBetweenStagesDays = 7;
    public const int CheckInterval = 250;
}
