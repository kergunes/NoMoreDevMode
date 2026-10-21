using System.Collections.Generic;
using RimWorld;
using Verse;

namespace EarnedResurrection;

// Preference snapshot and used archetypes belong to this save's parent quest.
public sealed class QuestPart_DlcPool : QuestPart
{
    public List<string> allowed = new List<string>();
    public List<string> used = new List<string>();

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref allowed, "allowed", LookMode.Value);
        Scribe_Collections.Look(ref used, "used", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (allowed == null) allowed = new List<string>();
            if (used == null) used = new List<string>();
        }
    }
}
