using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace arsiy.Rooms.WorldGen
{
    
    
    
    
    
    
    
    
    
    
    public class WorldGenStep_YellowRoomsTerrain : WorldGenStep
    {
        public override int SeedPart => 123456789;

        
        
        private const int PoolZoneCount = 8;
        private const int ParkingZoneCount = 6;
        
        
        
        
        
        private const float PoolClaimTiles = 5f;     
        private const float ParkingClaimTiles = 4f;  

        public override void GenerateFresh(string seed, PlanetLayer layer)
        {
            var yellowRooms = DefDatabase<BiomeDef>.GetNamedSilentFail("YellowRooms");
            if (yellowRooms == null)
            {
                RoomsLog.Error("[Rooms] YellowRooms biome def not found during world gen");
                return;
            }

            int tilesCount = layer.TilesCount;
            if (tilesCount <= 0) return;

            var poolRooms = DefDatabase<BiomeDef>.GetNamedSilentFail("YellowRooms_PoolRooms");
            var parking = DefDatabase<BiomeDef>.GetNamedSilentFail("YellowRooms_Parking");

            
            layer.Tiles.Clear();
            for (int i = 0; i < tilesCount; i++)
            {
                var planetTile = new PlanetTile(i, layer);
                var tile = new SurfaceTile(planetTile)
                {
                    elevation = 100f,
                    hilliness = Hilliness.Flat,
                    temperature = 22f,
                    rainfall = 0f,
                    swampiness = 0f
                };
                tile.PrimaryBiome = yellowRooms;
                layer.Tiles.Add(tile);
            }

            
            
            
            
            bool placed = false;
            try { placed = PlaceZonesByCentres(layer, poolRooms, parking, yellowRooms); }
            catch (Exception ex) { RoomsLog.Warning($"[Rooms] Spatial zone placement failed, using index fallback: {ex.Message}"); }

            if (!placed)
                PlaceZonesByIndex(layer, poolRooms, parking);

            int pool = 0, park = 0;
            foreach (var t in layer.Tiles)
            {
                if (t.PrimaryBiome == poolRooms) pool++;
                else if (t.PrimaryBiome == parking) park++;
            }
            RoomsLog.Message($"[Rooms] Biome zones placed: yellow={tilesCount - pool - park} pool={pool} parking={park} (total {tilesCount})");
        }

        
        
        
        private bool PlaceZonesByCentres(PlanetLayer layer, BiomeDef poolRooms, BiomeDef parking, BiomeDef yellowRooms)
        {
            int tilesCount = layer.TilesCount;

            
            
            var centres = new UnityEngine.Vector3[tilesCount];
            float avgTile = layer.AverageTileSize;
            if (avgTile <= 0.0001f) return false;
            for (int i = 0; i < tilesCount; i++)
            {
                var c = layer.GetTileCenter(i);
                if (c.sqrMagnitude < 0.0001f || float.IsNaN(c.x)) return false;
                centres[i] = c;
            }

            
            
            float poolR2 = (PoolClaimTiles * avgTile) * (PoolClaimTiles * avgTile);
            float parkR2 = (ParkingClaimTiles * avgTile) * (ParkingClaimTiles * avgTile);

            
            
            
            float minSepSq = (3f * avgTile) * (3f * avgTile);
            var poolCentres = PickSpreadCentres(centres, PoolZoneCount, layer.Radius, minSepSq: minSepSq);
            var parkCentres = PickSpreadCentres(centres, ParkingZoneCount, layer.Radius, avoid: poolCentres, minSepSq: minSepSq);

            for (int i = 0; i < tilesCount; i++)
            {
                var c = centres[i];
                
                
                if (parking != null && WithinAny(c, parkCentres, parkR2, centres))
                    layer.Tiles[i].PrimaryBiome = parking;
                else if (poolRooms != null && WithinAny(c, poolCentres, poolR2, centres))
                    layer.Tiles[i].PrimaryBiome = poolRooms;
                else
                    layer.Tiles[i].PrimaryBiome = yellowRooms;
            }
            return true;
        }

        
        
        private static List<int> PickSpreadCentres(UnityEngine.Vector3[] centres, int count, float layerRadius, List<int> avoid = null, float minSepSq = 0f)
        {
            var chosen = new List<int>();
            if (centres.Length == 0 || count <= 0) return chosen;
            
            chosen.Add(Rand.Range(0, centres.Length));
            for (int n = 1; n < count; n++)
            {
                int best = -1; float bestDist = -1f;
                for (int i = 0; i < centres.Length; i++)
                {
                    if (chosen.Contains(i)) continue;
                    
                    float minD = MinDistSq(centres[i], centres, chosen);
                    if (avoid != null) minD = Math.Min(minD, MinDistSq(centres[i], centres, avoid));
                    
                    if (minSepSq > 0f && minD < minSepSq) continue;
                    
                    minD *= Rand.Range(0.7f, 1.3f);
                    if (minD > bestDist) { bestDist = minD; best = i; }
                }
                if (best >= 0) chosen.Add(best);
            }
            return chosen;
        }

        private static float MinDistSq(UnityEngine.Vector3 c, UnityEngine.Vector3[] centres, List<int> idxs)
        {
            float best = float.MaxValue;
            foreach (var i in idxs)
            {
                float d = (centres[i] - c).sqrMagnitude;
                if (d < best) best = d;
            }
            return best;
        }

        private static bool WithinAny(UnityEngine.Vector3 c, List<int> centreIdxs, float r2, UnityEngine.Vector3[] centres)
        {
            foreach (var i in centreIdxs)
                if ((centres[i] - c).sqrMagnitude <= r2) return true;
            return false;
        }

        
        
        
        
        private void PlaceZonesByIndex(PlanetLayer layer, BiomeDef poolRooms, BiomeDef parking)
        {
            int tilesCount = layer.TilesCount;
            if (tilesCount <= 0) return;
            int poolZoneLen = Math.Max(1, tilesCount / 14);
            int parkZoneLen = Math.Max(1, tilesCount / 28);
            var assigned = new bool[tilesCount];

            if (poolRooms != null)
            {
                for (int z = 0; z < PoolZoneCount; z++)
                {
                    int start = Rand.Range(0, tilesCount);
                    for (int k = 0; k < poolZoneLen; k++)
                    {
                        int idx = (start + k) % tilesCount;
                        if (!assigned[idx]) { assigned[idx] = true; layer.Tiles[idx].PrimaryBiome = poolRooms; }
                    }
                }
            }
            if (parking != null)
            {
                for (int z = 0; z < ParkingZoneCount; z++)
                {
                    int start = Rand.Range(0, tilesCount);
                    for (int k = 0; k < parkZoneLen; k++)
                    {
                        int idx = (start + k) % tilesCount;
                        if (!assigned[idx]) { assigned[idx] = true; layer.Tiles[idx].PrimaryBiome = parking; }
                    }
                }
            }
        }
    }
}
