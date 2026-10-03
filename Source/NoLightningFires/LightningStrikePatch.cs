using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace NoLightningFires;

/// <summary>
/// Every lightning strike (thunderstorms, flashstorms, delayed strikes) goes through <c>DoStrike</c>, which sets off
/// a radius 1.9 <c>Flame</c> explosion. <c>Burn</c> is vanilla's child of <c>Flame</c> with the same damage, visuals
/// and sound, but its worker never starts or attaches fire. Swapping the def keeps the strike's damage and drops
/// the fire.
/// </summary>
[HarmonyPatch(typeof(WeatherEvent_LightningStrike), nameof(WeatherEvent_LightningStrike.DoStrike))]
public static class LightningStrikePatch
{
    private static readonly FieldInfo Flame = AccessTools.Field(typeof(DamageDefOf), nameof(DamageDefOf.Flame));
    private static readonly FieldInfo Burn = AccessTools.Field(typeof(DamageDefOf), nameof(DamageDefOf.Burn));

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        int swapped = 0;
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.LoadsField(Flame))
            {
                instruction.operand = Burn;
                swapped++;
            }
            yield return instruction;
        }
        if (swapped == 0)
        {
            Log.Error("[NoLightningFires] DamageDefOf.Flame not found in WeatherEvent_LightningStrike.DoStrike; lightning can still start fires.");
        }
    }
}
