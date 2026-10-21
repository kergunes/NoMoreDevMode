using Verse;
using HarmonyLib;
using UnityEngine;

namespace EarnedResurrection;

public sealed class ResurrectionMod : Mod
{
    public static ResurrectionSettings Settings { get; private set; }

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
        listing.Begin(inRect);
        listing.Label("ER_SettingsDelay".Translate());
        if (listing.RadioButton("ER_DelayImmediate".Translate(), Settings.delay == OpportunityDelay.Immediate))
            Settings.delay = OpportunityDelay.Immediate;
        if (listing.RadioButton("ER_DelayOneToTwoDays".Translate(), Settings.delay == OpportunityDelay.OneToTwoDays))
            Settings.delay = OpportunityDelay.OneToTwoDays;
        if (listing.RadioButton("ER_DelayWithinAWeek".Translate(), Settings.delay == OpportunityDelay.WithinAWeek))
            Settings.delay = OpportunityDelay.WithinAWeek;
        listing.Gap();
        listing.Label("ER_SettingsMinimumMembership".Translate(Settings.minimumMembershipDays));
        Settings.minimumMembershipDays = Mathf.RoundToInt(listing.Slider(Settings.minimumMembershipDays, 0f, 60f));
        listing.Label("ER_SettingsMembershipExplanation".Translate());
        listing.Gap();
        listing.Label("ER_SettingsTimingExplanation".Translate());
        listing.End();
    }
}
