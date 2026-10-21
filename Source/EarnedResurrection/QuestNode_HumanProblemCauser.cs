using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace EarnedResurrection;

// Reuse the vanilla device quest, but never its mech-cluster branch. Build a
// temporary node list; do not mutate the shared vanilla QuestScriptDef/tree.
public sealed class QuestNode_HumanProblemCauser : QuestNode
{
    private static QuestScriptDef Source => DefDatabase<QuestScriptDef>.GetNamed("ProblemCauser");

    private static QuestNode_Sequence HumanSequence(Slate slate)
    {
        slate.Set("selectionWeightMechanoid", 0f);
        var native = (QuestNode_Sequence)Source.root;
        return new QuestNode_Sequence
        {
            nodes = native.nodes.Where(n => !(n is QuestNode_FactionExists check) ||
                check.faction.GetValue(slate)?.def.defName != "Mechanoid").ToList()
        };
    }

    protected override bool TestRunInt(Slate slate) => ModsConfig.RoyaltyActive &&
        Find.FactionManager.AllFactions.Any(f => f.def.defName == "Pirate" && !f.defeated) &&
        HumanSequence(slate).TestRun(slate);

    protected override void RunInt()
    {
        Source.InitializeRules();
        HumanSequence(QuestGen.slate).Run();
    }
}
