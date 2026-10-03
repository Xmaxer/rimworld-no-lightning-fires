using System.Collections.Generic;
using System.IO;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace NoLightningFires.DevHarness;

public static class HarnessUtil
{
    public static string HarnessDir => Path.Combine(GenFilePaths.SaveDataFolderPath, "LightningHarness");

    private static readonly string[] AllowedErrorFragments =
    {
        // Environment noise: duplicate Workshop subscriptions of unrelated mods on the dev machine.
        "Tried loading mod with the same packageId multiple times"
    };

    /// <summary>26x26 areas 40 cells apart, so a fire in one cannot spread into the next.</summary>
    public static CellRect ReserveArea(Map map, int index)
    {
        int x0 = 10 + (index % 5) * 40;
        int z0 = 10 + (index / 5) * 40;
        CellRect rect = new CellRect(x0, z0, 26, 26).ClipInsideMap(map);
        FlattenCells(map, rect);
        return rect;
    }

    /// <summary>Concretes, unroofs and unfogs cells, destroying anything natural (and non-colonist) on them.</summary>
    public static void FlattenCells(Map map, IEnumerable<IntVec3> cells)
    {
        List<Thing> doomed = new List<Thing>();
        foreach (IntVec3 c in cells)
        {
            if (!c.InBounds(map))
            {
                continue;
            }
            List<Thing> things = c.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                // Non-destroyable things (steam geysers, map-feature buildings) log an error when destroyed.
                if (things[i].def.destroyable && (!(things[i] is Pawn p) || p.Faction != Faction.OfPlayer))
                {
                    doomed.Add(things[i]);
                }
            }
            map.terrainGrid.SetTerrain(c, TerrainDefOf.Concrete);
            map.roofGrid.SetRoof(c, null);
            map.fogGrid.Unfog(c);
        }
        for (int i = 0; i < doomed.Count; i++)
        {
            if (!doomed[i].Destroyed)
            {
                doomed[i].Destroy(DestroyMode.Vanish);
            }
        }
    }

    /// <summary>Sets the weather with no transition. Tests use dry weather so rain never puts out a fire.</summary>
    public static void SetWeather(Map map, WeatherDef weather)
    {
        map.weatherManager.lastWeather = weather;
        map.weatherManager.curWeather = weather;
        map.weatherManager.curWeatherAge = 10000;
    }

    /// <summary>Fires on a cell in <paramref name="rect"/> or attached to something standing in it.</summary>
    public static int FiresIn(Map map, CellRect rect)
    {
        int count = 0;
        foreach (Thing t in map.listerThings.ThingsOfDef(ThingDefOf.Fire))
        {
            Fire fire = (Fire)t;
            if (rect.Contains(fire.Position) || (fire.parent != null && rect.Contains(fire.parent.Position)))
            {
                count++;
            }
        }
        return count;
    }

    public static void PutOutFires(Map map)
    {
        List<Thing> fires = new List<Thing>(map.listerThings.ThingsOfDef(ThingDefOf.Fire));
        foreach (Thing fire in fires)
        {
            if (!fire.Destroyed)
            {
                fire.Destroy(DestroyMode.Vanish);
            }
        }
    }

    public static Thing SpawnBuilding(ThingDef def, IntVec3 cell, Map map, ThingDef stuff = null)
    {
        Thing thing = ThingMaker.MakeThing(def, stuff ?? (def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null));
        if (def.CanHaveFaction)
        {
            thing.SetFactionDirect(Faction.OfPlayer);
        }
        return GenSpawn.Spawn(thing, cell, map, Rot4.North, WipeMode.Vanish);
    }

    /// <summary>A naked adult colonist, so strike damage lands on the body instead of clothing.</summary>
    public static Pawn SpawnNakedColonist(IntVec3 cell, Map map)
    {
        PawnGenerationRequest request = new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer,
            forceGenerateNewPawn: true, canGeneratePawnRelations: false, allowAddictions: false,
            developmentalStages: DevelopmentalStage.Adult);
        Pawn pawn = PawnGenerator.GeneratePawn(request);
        pawn.apparel?.DestroyAll();
        GenSpawn.Spawn(pawn, cell, map);
        return pawn;
    }

    public static bool HasBurn(Pawn pawn)
    {
        foreach (Hediff h in pawn.health.hediffSet.hediffs)
        {
            if (h.def == DamageDefOf.Burn.hediff)
            {
                return true;
            }
        }
        return false;
    }

    public static void FocusCamera(IntVec3 cell, float size)
    {
        Find.CameraDriver.SetRootPosAndSize(cell.ToVector3Shifted(), size);
    }

    public static void Screenshot(string name)
    {
        Directory.CreateDirectory(HarnessDir);
        EditWindow_Log log = Find.WindowStack.WindowOfType<EditWindow_Log>();
        if (log != null)
        {
            Find.WindowStack.TryRemove(log, doCloseSound: false);
        }
        ScreenCapture.CaptureScreenshot(Path.Combine(HarnessDir, name + ".png"));
    }

    public static void CloseBlockingWindows()
    {
        IList<Window> windows = Find.WindowStack.Windows;
        for (int i = windows.Count - 1; i >= 0; i--)
        {
            Window w = windows[i];
            if (w.forcePause || w.absorbInputAroundWindow)
            {
                Find.WindowStack.TryRemove(w, doCloseSound: false);
            }
        }
    }

    public static List<string> UnexpectedLoggedErrors()
    {
        List<string> result = new List<string>();
        using (Log.LockMessages())
        {
            foreach (LogMessage m in Log.Messages)
            {
                if (m.type != LogMessageType.Error)
                {
                    continue;
                }
                bool allowed = false;
                for (int i = 0; i < AllowedErrorFragments.Length; i++)
                {
                    if (m.text != null && m.text.Contains(AllowedErrorFragments[i]))
                    {
                        allowed = true;
                        break;
                    }
                }
                if (!allowed)
                {
                    result.Add(m.text);
                }
            }
        }
        return result;
    }

    public static string JsonEscape(string s)
    {
        if (s == null)
        {
            return "";
        }
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", " ").Replace("\n", " ");
    }
}

public static class Check
{
    public static void Eq<T>(List<string> failures, string what, T actual, T expected)
    {
        if (!EqualityComparer<T>.Default.Equals(actual, expected))
        {
            failures.Add($"{what}: expected {expected}, got {actual}");
        }
    }

    public static void True(List<string> failures, string what, bool condition)
    {
        if (!condition)
        {
            failures.Add(what);
        }
    }
}
