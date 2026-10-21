using Verse;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace EarnedResurrection;

public sealed class ResurrectionMod : Mod
{
    public static ResurrectionSettings Settings { get; private set; }
    private Vector2 settingsScroll;
    private float settingsHeight = 800f;
    private readonly HashSet<string> expandedDlc = new HashSet<string>();

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
        // A scroll view must grow vertically. Listing otherwise wraps overflow
        // into an invisible second column, shrinking CurHeight on the next draw.
        var listing = new Listing_Standard { maxOneColumn = true };
        settingsHeight = Mathf.Max(settingsHeight, inRect.height);
        settingsScroll.y = Mathf.Clamp(settingsScroll.y, 0f, Mathf.Max(0f, settingsHeight - inRect.height));
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
        listing.Label("ER_SettingsDlcPools".Translate());
        if (ModsConfig.RoyaltyActive)
            DrawDlcPool(listing, "Royalty", ref Settings.royaltyQuests);
        if (ModsConfig.IdeologyActive)
            DrawDlcPool(listing, "Ideology", ref Settings.ideologyQuests);
        if (ModsConfig.BiotechActive)
            DrawDlcPool(listing, "Biotech", ref Settings.biotechQuests);
        if (ModsConfig.OdysseyActive)
            DrawDlcPool(listing, "Odyssey", ref Settings.odysseyQuests);
        if (ModsConfig.AnomalyActive)
            listing.Label("ER_SettingsAnomalyExcluded".Translate());
        listing.Label("ER_SettingsDlcPoolsExplanation".Translate());
        listing.Gap();
        listing.Label("ER_SettingsTimingExplanation".Translate());
        settingsHeight = Mathf.Max(inRect.height, listing.CurHeight + 20f);
        listing.End();
        Widgets.EndScrollView();
    }

    private void DrawDlcPool(Listing_Standard listing, string dlc, ref bool enabled)
    {
        listing.CheckboxLabeled(dlc, ref enabled);
        bool expanded = expandedDlc.Contains(dlc);
        if (listing.ButtonText((expanded ? "ER_SettingsHideDlcQuests" : "ER_SettingsShowDlcQuests").Translate(dlc)))
        {
            if (expanded) expandedDlc.Remove(dlc);
            else expandedDlc.Add(dlc);
            expanded = !expanded;
        }
        if (expanded)
        {
            foreach (var entry in DlcLeadCatalog.Entries)
            {
                if (entry.dlc != dlc) continue;
                bool selected = !Settings.disabledDlcLeads.Contains(entry.name);
                listing.CheckboxLabeled(("ER_DlcName_" + entry.name).Translate(), ref selected);
                if (selected) Settings.disabledDlcLeads.Remove(entry.name);
                else if (!Settings.disabledDlcLeads.Contains(entry.name)) Settings.disabledDlcLeads.Add(entry.name);
            }
        }
        listing.Gap();
    }
}
