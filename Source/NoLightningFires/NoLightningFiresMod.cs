using Verse;

namespace NoLightningFires;

public class NoLightningFiresMod : Mod
{
    public const string HarmonyId = "Xmaxer.NoLightningFires";

    public NoLightningFiresMod(ModContentPack content) : base(content)
    {
        new HarmonyLib.Harmony(HarmonyId).PatchAll(typeof(NoLightningFiresMod).Assembly);
    }
}
