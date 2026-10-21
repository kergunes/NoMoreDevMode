using Verse;
using HarmonyLib;
using UnityEngine;

namespace EarnedResurrection;

public sealed class ResurrectionMod : Mod
{
    public static ResurrectionSettings Settings { get; private set; }
    private Vector2 settingsScroll;
    private float settingsHeight = 800f;

    public ResurrectionMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<ResurrectionSettings>();
        Settings.ClampValues();
        new Harmony("kergunes.earnedresurrection").PatchAll();
        Log.Message("[EarnedResurrection] NoMoreDevMode / Earned Resurrection loaded.");
    }

    public override string SettingsCategory() => "NoMoreDevMode / Earned Resurrection";

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        var viewRect = new Rect(0f, 0f, inRect.width - 20f, settingsHeight);
        Widgets.BeginScrollView(inRect, ref settingsScroll, viewRect);
        listing.Begin(viewRect);
        listing.Label("ER_SettingsDelay".Translate());
        if (listing.RadioButton("ER_DelayImmediate".Translate(), Settings.delay == OpportunityDelay.Immediate))
            Settings.delay = OpportunityDelay.Immediate;
        if (listing.RadioButton("ER_DelayOneToTwoDays".Translate(), Settings.delay == OpportunityDelay.OneToTwoDays))
            Settings.delay = OpportunityDelay.OneToTwoDays;
        if (listing.RadioButton("ER_DelayWithinAWeek".Translate(), Settings.delay == OpportunityDelay.WithinAWeek))
            Settings.delay = OpportunityDelay.WithinAWeek;
        listing.Gap();
        listing.Label("ER_SettingsLeadCount".Translate(Settings.leadCount));
        Settings.leadCount = Mathf.RoundToInt(listing.Slider(Settings.leadCount, 1f, 5f));
        listing.Label("ER_SettingsLeadCountExplanation".Translate());
        listing.Gap();
        listing.Label("ER_SettingsBetweenStages".Translate(Settings.betweenStagesDays));
        Settings.betweenStagesDays = Mathf.RoundToInt(listing.Slider(Settings.betweenStagesDays, 0f,
            ResurrectionTuning.MaximumBetweenStagesDays));
        listing.Label("ER_SettingsBetweenStagesExplanation".Translate());
        listing.Gap();
        listing.Label("ER_SettingsMinimumMembership".Translate(Settings.minimumMembershipDays));
        Settings.minimumMembershipDays = Mathf.RoundToInt(listing.Slider(Settings.minimumMembershipDays, 0f, 60f));
        listing.Label("ER_SettingsMembershipExplanation".Translate());
        listing.Gap();
        listing.Label("ER_SettingsCooldown".Translate(Settings.cooldownDays));
        Settings.cooldownDays = Mathf.RoundToInt(listing.Slider(Settings.cooldownDays, 0f,
            ResurrectionTuning.MaximumCooldownDays));
        listing.Label("ER_SettingsCooldownExplanation".Translate());
        listing.Gap();
        listing.Label("ER_SettingsTimingExplanation".Translate());
        settingsHeight = listing.CurHeight + 20f;
        listing.End();
        Widgets.EndScrollView();
    }
}
