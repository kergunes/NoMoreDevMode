using Verse;
using HarmonyLib;

namespace EarnedResurrection;

public sealed class ResurrectionMod : Mod
{
    public ResurrectionMod(ModContentPack content) : base(content)
    {
        new Harmony("kergunes.earnedresurrection").PatchAll();
        Log.Message("[EarnedResurrection] NoMoreDevMode / Earned Resurrection loaded.");
    }
}
