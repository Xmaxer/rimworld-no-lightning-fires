using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace NoLightningFires.DevHarness;

/// <summary>Records every lightning strike cell, so scenarios can wait for natural strikes and see where they hit.</summary>
[StaticConstructorOnStartup]
public static class StrikeLog
{
    public static readonly List<IntVec3> Strikes = new List<IntVec3>();

    static StrikeLog()
    {
        new Harmony("Xmaxer.NoLightningFires.DevHarness").Patch(
            AccessTools.Method(typeof(WeatherEvent_LightningStrike), nameof(WeatherEvent_LightningStrike.DoStrike)),
            postfix: new HarmonyMethod(typeof(StrikeLog), nameof(Postfix)));
    }

    // DoStrike overwrites an invalid strikeLoc with the random cell it picked, and the postfix sees that value.
    private static void Postfix(IntVec3 strikeLoc)
    {
        Strikes.Add(strikeLoc);
    }

    /// <summary>Calls the patched strike directly at a cell, as a thunderstorm or flashstorm would.</summary>
    public static void Strike(Map map, IntVec3 cell)
    {
        Mesh mesh = null;
        WeatherEvent_LightningStrike.DoStrike(cell, map, ref mesh);
    }
}
