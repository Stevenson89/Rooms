using RimWorld;
using Verse;

namespace arsiy.Rooms.GenSteps
{
    public class GenStep_YellowRoomsFloor : GenStep
    {
        public override int SeedPart => 876543210;

        public override void Generate(Map map, GenStepParams parms)
        {
            var ceramic = DefDatabase<TerrainDef>.GetNamed("CeramicTile");
            var carpet = DefDatabase<TerrainDef>.GetNamed("CarpetYellow");
            if (ceramic == null || carpet == null) return;
            foreach (var cell in map.AllCells)
            {
                map.terrainGrid.SetUnderTerrain(cell, ceramic);
                map.terrainGrid.SetTerrain(cell, carpet);
            }
        }
    }
}
