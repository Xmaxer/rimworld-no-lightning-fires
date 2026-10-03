using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace NoLightningFires.DevHarness.Scenarios;

/// <summary>The transpiler is applied to DoStrike, and Burn still uses a worker that cannot ignite.</summary>
public sealed class BootScenario : HarnessScenario
{
    public override string Name => "boot";

    public override void Setup(Map map, CellRect area)
    {
    }

    public override ScenarioStatus Tick(Map map, int ticksSinceSetup, List<string> failures)
    {
        Patches info = Harmony.GetPatchInfo(AccessTools.Method(typeof(WeatherEvent_LightningStrike), nameof(WeatherEvent_LightningStrike.DoStrike)));
        bool patched = false;
        if (info != null)
        {
            foreach (Patch p in info.Transpilers)
            {
                patched |= p.owner == NoLightningFiresMod.HarmonyId;
            }
        }
        Check.True(failures, "DoStrike transpiler not applied", patched);
        Check.True(failures, "Burn uses DamageWorker_Flame", !(DamageDefOf.Burn.Worker is DamageWorker_Flame));
        return failures.Count == 0 ? ScenarioStatus.Passed : ScenarioStatus.Failed;
    }
}

/// <summary>
/// Nine direct strikes on a grass field: one on a naked colonist, one beside a wooden wall, one beside wood logs.
/// Nothing may catch fire, but the colonist gets a burn and the wall loses hit points.
/// </summary>
public sealed class StrikeScenario : HarnessScenario
{
    private readonly List<IntVec3> cells = new List<IntVec3>();
    private CellRect field;
    private Pawn pawn;
    private Thing wall;

    public override string Name => "strike";

    public override int TimeoutTicks => 1000;

    public override void Setup(Map map, CellRect area)
    {
        HarnessUtil.SetWeather(map, WeatherDefOf.Clear);
        field = area;
        Field.Build(map, field);
        IntVec3 center = field.CenterCell;
        pawn = HarnessUtil.SpawnNakedColonist(center, map);
        wall = new IntVec3(field.minX + 2, 0, field.minZ + 1).GetEdifice(map);
        cells.Add(center);
        cells.Add(new IntVec3(field.minX + 2, 0, field.minZ + 2));
        cells.Add(new IntVec3(field.minX + 2, 0, field.maxZ - 2));
        for (int i = 0; i < 6; i++)
        {
            cells.Add(new IntVec3(field.minX + 4 + i * 3, 0, field.minZ + 6 + (i % 3) * 5));
        }
    }

    public override ScenarioStatus Tick(Map map, int ticksSinceSetup, List<string> failures)
    {
        if (ticksSinceSetup % 10 == 0 && ticksSinceSetup / 10 < cells.Count)
        {
            StrikeLog.Strike(map, cells[ticksSinceSetup / 10]);
        }
        int fires = HarnessUtil.FiresIn(map, field.ExpandedBy(2));
        if (fires > 0)
        {
            failures.Add($"{fires} fire(s) in the field at tick {ticksSinceSetup}");
            return ScenarioStatus.Failed;
        }
        if (pawn.IsBurning())
        {
            failures.Add("colonist is burning");
            return ScenarioStatus.Failed;
        }
        if (ticksSinceSetup < 600)
        {
            return ScenarioStatus.Running;
        }
        Metrics.Add($"strikes={cells.Count}");
        Check.True(failures, "colonist hit by the strike has no burn", HasBurnOrDied(pawn));
        Check.True(failures, "wooden wall beside a strike took no damage", wall == null || wall.Destroyed || wall.HitPoints < wall.MaxHitPoints);
        Check.True(failures, "wooden wall missing from the field", wall != null);
        if (wall != null && !wall.Destroyed)
        {
            Metrics.Add($"wall={wall.HitPoints}/{wall.MaxHitPoints}");
        }
        return failures.Count == 0 ? ScenarioStatus.Passed : ScenarioStatus.Failed;
    }

    private static bool HasBurnOrDied(Pawn p)
    {
        return p.Dead || HarnessUtil.HasBurn(p);
    }

    public override void Cleanup(Map map)
    {
        if (pawn != null && !pawn.Destroyed)
        {
            pawn.Destroy(DestroyMode.Vanish);
        }
    }
}

/// <summary>A real flashstorm centred on a grass field: wait for six of its strikes, with no fire at any point.</summary>
public sealed class FlashstormScenario : HarnessScenario
{
    private const int Wanted = 6;
    private GameCondition_Flashstorm storm;
    private CellRect field;
    private int startStrikes;

    public override string Name => "flashstorm";

    public override int TimeoutTicks => 9000;

    public override void Setup(Map map, CellRect area)
    {
        HarnessUtil.SetWeather(map, WeatherDefOf.Clear);
        field = area;
        Field.Build(map, field);
        storm = Playground.StartFlashstorm(map, field.CenterCell, 9, 20000);
        startStrikes = StrikeLog.Strikes.Count;
    }

    public override ScenarioStatus Tick(Map map, int ticksSinceSetup, List<string> failures)
    {
        int fires = HarnessUtil.FiresIn(map, field.ExpandedBy(2));
        if (fires > 0)
        {
            failures.Add($"{fires} fire(s) in the field at tick {ticksSinceSetup}");
            return ScenarioStatus.Failed;
        }
        int strikes = StrikeLog.Strikes.Count - startStrikes;
        if (strikes < Wanted)
        {
            return ScenarioStatus.Running;
        }
        Metrics.Add($"strikes={strikes}");
        for (int i = startStrikes; i < StrikeLog.Strikes.Count; i++)
        {
            Check.True(failures, $"strike {StrikeLog.Strikes[i]} landed outside the field", field.Contains(StrikeLog.Strikes[i]));
        }
        return failures.Count == 0 ? ScenarioStatus.Passed : ScenarioStatus.Failed;
    }

    public override void Cleanup(Map map)
    {
        storm?.End();
    }
}

/// <summary>
/// A dry thunderstorm over the whole unfogged map: wait for three natural strikes wherever they land, with no fire
/// anywhere on the map. Dry thunderstorms have no rain to put fires out.
/// </summary>
public sealed class StormScenario : HarnessScenario
{
    private const int Wanted = 3;
    private WeatherDef dry;
    private int startStrikes;

    public override string Name => "storm";

    public override int TimeoutTicks => 20000;

    public override void Setup(Map map, CellRect area)
    {
        map.fogGrid.ClearAllFog();
        dry = DefDatabase<WeatherDef>.GetNamed("DryThunderstorm");
        HarnessUtil.SetWeather(map, dry);
        startStrikes = StrikeLog.Strikes.Count;
    }

    public override ScenarioStatus Tick(Map map, int ticksSinceSetup, List<string> failures)
    {
        // The weather decider would move on after the def's duration; keep the storm going until enough strikes.
        if (map.weatherManager.curWeather != dry)
        {
            HarnessUtil.SetWeather(map, dry);
        }
        int fires = map.listerThings.ThingsOfDef(ThingDefOf.Fire).Count;
        if (fires > 0)
        {
            failures.Add($"{fires} fire(s) on the map at tick {ticksSinceSetup}");
            return ScenarioStatus.Failed;
        }
        int strikes = StrikeLog.Strikes.Count - startStrikes;
        if (strikes < Wanted)
        {
            return ScenarioStatus.Running;
        }
        for (int i = startStrikes; i < StrikeLog.Strikes.Count; i++)
        {
            IntVec3 c = StrikeLog.Strikes[i];
            Plant plant = c.GetPlant(map);
            Metrics.Add($"strike {c}: terrain={c.GetTerrain(map).defName} plant={plant?.def.defName ?? "none"}");
        }
        return ScenarioStatus.Passed;
    }

    public override void Cleanup(Map map)
    {
        HarnessUtil.SetWeather(map, WeatherDefOf.Clear);
    }
}

/// <summary>
/// Proves the field burns: the same radius 1.9 Flame explosion a vanilla strike uses, set off directly (not through
/// DoStrike), must start fires. Runs last, and puts every fire out afterwards.
/// </summary>
public sealed class ControlScenario : HarnessScenario
{
    private CellRect field;

    public override string Name => "control";

    public override int TimeoutTicks => 300;

    public override void Setup(Map map, CellRect area)
    {
        HarnessUtil.SetWeather(map, WeatherDefOf.Clear);
        field = area;
        Field.Build(map, field);
        for (int i = 0; i < 3; i++)
        {
            GenExplosion.DoExplosion(new IntVec3(field.minX + 5 + i * 7, 0, field.CenterCell.z), map, 1.9f, DamageDefOf.Flame, null);
        }
    }

    public override ScenarioStatus Tick(Map map, int ticksSinceSetup, List<string> failures)
    {
        if (ticksSinceSetup < 60)
        {
            return ScenarioStatus.Running;
        }
        int fires = HarnessUtil.FiresIn(map, field.ExpandedBy(2));
        Metrics.Add($"fires={fires}");
        Check.True(failures, "a vanilla Flame explosion started no fire: the field is not a valid test", fires > 0);
        return failures.Count == 0 ? ScenarioStatus.Passed : ScenarioStatus.Failed;
    }

    public override void Cleanup(Map map)
    {
        HarnessUtil.PutOutFires(map);
    }
}

/// <summary>
/// Interactive dev convenience, not a test (tools/play.ps1 requests it). On a new colony: a grass field with wood
/// logs and wooden walls next to the colonists, and a day-long flashstorm over it. Control is then handed back to
/// the player, paused.
/// </summary>
public sealed class PlaygroundScenario : HarnessScenario
{
    public override string Name => "playground";

    public override bool IsPersistent => true;

    public override void Setup(Map map, CellRect area)
    {
        IntVec3 colonists = Playground.ColonistCenter(map);
        CellRect field = new CellRect(colonists.x + 6, colonists.z - 10, 26, 20).ClipInsideMap(map);
        Field.Build(map, field);
        HarnessUtil.SetWeather(map, WeatherDefOf.Clear);
        Playground.StartFlashstorm(map, field.CenterCell, 9, GenDate.TicksPerDay);
        HarnessUtil.FocusCamera(field.CenterCell, 30f);
    }

    public override ScenarioStatus Tick(Map map, int ticksSinceSetup, List<string> failures)
    {
        return ScenarioStatus.Passed;
    }
}

public static class Playground
{
    public static GameCondition_Flashstorm StartFlashstorm(Map map, IntVec3 center, int radius, int duration)
    {
        GameCondition_Flashstorm storm = (GameCondition_Flashstorm)GameConditionMaker.MakeCondition(GameConditionDefOf.Flashstorm, duration);
        storm.centerLocation = new IntVec2(center.x, center.z);
        storm.areaRadiusOverride = new IntRange(radius, radius);
        map.gameConditionManager.RegisterCondition(storm);
        return storm;
    }

    public static IntVec3 ColonistCenter(Map map)
    {
        List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
        if (colonists.Count == 0)
        {
            return map.Center;
        }
        int x = 0;
        int z = 0;
        foreach (Pawn p in colonists)
        {
            x += p.Position.x;
            z += p.Position.z;
        }
        return new IntVec3(x / colonists.Count, 0, z / colonists.Count);
    }
}
