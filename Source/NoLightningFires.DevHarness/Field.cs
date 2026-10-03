using RimWorld;
using Verse;

namespace NoLightningFires.DevHarness;

/// <summary>
/// Tinder: soil covered in fully grown grass, with wood log stacks along the top and wooden walls along the bottom.
/// A vanilla lightning strike anywhere on it sets it alight, which the control scenario checks.
/// </summary>
public static class Field
{
    public static void Build(Map map, CellRect rect)
    {
        HarnessUtil.FlattenCells(map, rect);
        foreach (IntVec3 c in rect)
        {
            map.terrainGrid.SetTerrain(c, TerrainDefOf.Soil);
            Plant grass = (Plant)ThingMaker.MakeThing(ThingDefOf.Plant_Grass);
            grass.Growth = 1f;
            GenSpawn.Spawn(grass, c, map);
        }
        for (int x = rect.minX + 2; x <= rect.maxX - 2; x += 5)
        {
            Thing wood = ThingMaker.MakeThing(ThingDefOf.WoodLog);
            wood.stackCount = 75;
            GenSpawn.Spawn(wood, new IntVec3(x, 0, rect.maxZ - 1), map, WipeMode.Vanish);
        }
        for (int x = rect.minX + 2; x <= rect.maxX - 2; x += 6)
        {
            HarnessUtil.SpawnBuilding(ThingDefOf.Wall, new IntVec3(x, 0, rect.minZ + 1), map, ThingDefOf.WoodLog);
        }
    }
}
