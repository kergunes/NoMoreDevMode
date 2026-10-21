using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace EarnedResurrection;

// An explicit allow-list: never enumerate all installed quest roots. In
// particular, no Anomaly, orbital, gravcore, bossgroup or mech-cluster roots.
internal static class DlcLeadCatalog
{
    internal sealed class Entry
    {
        public string name;
        public string dlc;
        public float weight;
        public bool archiveSite;
        public Entry(string name, string dlc, float weight = 3f, bool archiveSite = false)
        { this.name = name; this.dlc = dlc; this.weight = weight; this.archiveSite = archiveSite; }
    }

    internal static readonly List<Entry> Entries = new List<Entry>
    {
        new Entry("Mission_BanditCamp", "Royalty"),
        new Entry("ProblemCauser", "Royalty"),
        new Entry("ShuttleCrash_Rescue", "Royalty"),
        new Entry("ThreatReward_Raid_MiscReward", "Royalty"),
        new Entry("Hospitality_Joiners", "Royalty", 1f),
        new Entry("ER_ArchiveComplex", "Ideology"),
        new Entry("ER_ArchiveTerminal", "Ideology"),
        new Entry("ER_ArchiveDrone", "Ideology", 2f),
        new Entry("AncientComplex_Mission", "Ideology"),
        new Entry("OpportunitySite_WorkSite", "Ideology", archiveSite: true),
        new Entry("Opportunity_AncientStructureGarrison", "Odyssey", archiveSite: true),
        new Entry("Opportunity_AncientStructureLaunchSite", "Odyssey", archiveSite: true),
        new Entry("Opportunity_AncientStructureChemfuelRefinery", "Odyssey", archiveSite: true),
        new Entry("Opportunity_AncientStructureWarehouse", "Odyssey", archiveSite: true),
        new Entry("Opportunity_AncientInfestedSettlement", "Odyssey", archiveSite: true)
    };

    internal static bool Enabled(string dlc)
    {
        var settings = ResurrectionMod.Settings;
        switch (dlc)
        {
            case "Royalty": return ModsConfig.RoyaltyActive && (settings?.royaltyQuests ?? true);
            case "Ideology": return ModsConfig.IdeologyActive && (settings?.ideologyQuests ?? true);
            case "Biotech": return ModsConfig.BiotechActive && (settings?.biotechQuests ?? true);
            case "Odyssey": return ModsConfig.OdysseyActive && (settings?.odysseyQuests ?? true);
            default: return false;
        }
    }

    internal static List<string> EnabledNames() => Entries.Where(e => Enabled(e.dlc) &&
        !(ResurrectionMod.Settings?.disabledDlcLeads.Contains(e.name) ?? false) &&
        DefDatabase<QuestScriptDef>.GetNamedSilentFail(e.name) != null).Select(e => e.name).ToList();

    internal static bool ContainsRoot(string root) => Entries.Any(e => e.name == root ||
        e.name == "ProblemCauser" && root == "ER_HumanProblemCauser");
}
