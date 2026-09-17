using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace arsiy.Rooms.ScenParts
{
    // Like vanilla ScenPart_ForcedMap (which the Gravship scenario uses to start
    // on the Orbit layer), but the starting tile is guaranteed to be in the
    // core YellowRooms biome instead of PoolRooms or Parking.
    public class ScenPart_YellowRoomsForcedMap : ScenPart_ForcedMap
    {
        public override void PostWorldGenerate()
        {
            PlanetTile planetTile = PlanetTile.Invalid;
            PlanetLayer planetLayer = (layerDef != null) ? Find.WorldGrid.FirstLayerOfDef(layerDef) : null;
            if (planetLayer != null)
            {
                BiomeDef wantedBiome = DefDatabase<BiomeDef>.GetNamedSilentFail("YellowRooms");
                List<int> candidates = null;
                if (wantedBiome != null)
                {
                    candidates = new List<int>();
                    for (int i = 0; i < planetLayer.TilesCount; i++)
                    {
                        if (planetLayer[i].PrimaryBiome == wantedBiome)
                            candidates.Add(i);
                    }
                }
                if (candidates != null && candidates.Count > 0)
                {
                    planetTile = new PlanetTile(candidates[Rand.Range(0, candidates.Count)], planetLayer);
                }
                else
                {
                    planetTile = planetLayer.GetClosestTile_NewTemp(TileFinder.RandomStartingTile(), validSettlement: true);
                }
            }
            else
            {
                planetTile = TileFinder.RandomStartingTile();
            }
            Find.GameInitData.startingTile = planetTile;
            Find.GameInitData.mapGeneratorDef = mapGenerator;
        }
    }
}
