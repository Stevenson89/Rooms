using System.Collections.Generic;
using RimWorld;
using Verse;

namespace arsiy.Rooms.GenSteps
{
    
    
    
    
    
    
    
    
    
    
    
    
    public class GenStep_YellowRoomsSpecialSpots : GenStep
    {
        public override int SeedPart => 741085293;

        
        private const int WetRadius = 3;
        private const int WetRadiusSq = WetRadius * WetRadius;
        private const int LampFloodDepth = 10;      
        private const int SpotCountMin = 4;
        private const int SpotCountMax = 8;
        
        private const int MinSpotSeparation = 14;
        
        private const int OneExitRoomMinSize = 4;
        private const int OneExitRoomMaxSize = 64;

        private static readonly IntVec3[] Cardinals =
        {
            new IntVec3(1, 0, 0), new IntVec3(-1, 0, 0),
            new IntVec3(0, 0, 1), new IntVec3(0, 0, -1)
        };

        public override void Generate(Map map, GenStepParams parms)
        {
            var biome = map.Biome;
            if (biome == null || biome.defName != "YellowRooms") return;

            var wetDef = DefDatabase<TerrainDef>.GetNamedSilentFail("CarpetYellowWet");
            var carpetDef = DefDatabase<TerrainDef>.GetNamedSilentFail("CarpetYellow");
            var mushroomDef = ThingDef.Named("YellowRooms_Plant_EdibleMushroom");
            if (wetDef == null || carpetDef == null) return;

            var anchors = FindDeadEndAnchors(map, carpetDef);

            
            
            
            if (anchors.Count < SpotCountMax)
            {
                var fallbackPool = new List<IntVec3>();
                foreach (var cell in map.AllCells)
                {
                    if (!cell.InBounds(map)) continue;
                    if (!cell.Standable(map)) continue;
                    if (map.terrainGrid.TerrainAt(cell) != carpetDef) continue;
                    if (cell.GetEdifice(map) != null) continue;
                    fallbackPool.Add(cell);
                }
                var shuffled = InRandomOrder(fallbackPool);
                foreach (var c in shuffled)
                {
                    if (anchors.Count >= SpotCountMax) break;
                    if (!anchors.Contains(c)) anchors.Add(c);
                }
                RoomsLog.Message($"[Rooms] SpecialSpots: added {anchors.Count} anchors (dead-end gave fewer than {SpotCountMax})");
            }

            if (anchors.Count == 0)
            {
                RoomsLog.Message("[Rooms] SpecialSpots: no anchors found; skipping.");
                return;
            }

            
            int target = Rand.RangeInclusive(SpotCountMin, SpotCountMax);
            var chosen = new List<IntVec3>();
            var order = InRandomOrder(anchors);
            foreach (var c in order)
            {
                if (chosen.Count >= target) break;
                bool tooClose = false;
                foreach (var a in chosen)
                    if (Chebyshev(a, c) < MinSpotSeparation) { tooClose = true; break; }
                if (!tooClose) chosen.Add(c);
            }
            
            if (chosen.Count < SpotCountMin)
            {
                foreach (var c in order)
                {
                    if (chosen.Count >= SpotCountMin) break;
                    if (!chosen.Contains(c)) chosen.Add(c);
                }
            }

            foreach (var anchor in chosen)
                MakeSpot(map, anchor, wetDef, carpetDef, mushroomDef);

            RoomsLog.Message($"[Rooms] SpecialSpots: placed {chosen.Count} spot(s) at {string.Join(", ", chosen)}");
        }

        
        
        
        
        
        private static List<IntVec3> FindDeadEndAnchors(Map map, TerrainDef carpetDef)
        {
            var anchors = new List<IntVec3>();

            
            var regionId = new Dictionary<IntVec3, int>();
            var regions = new List<List<IntVec3>>();
            foreach (var cell in map.AllCells)
            {
                if (regionId.ContainsKey(cell)) continue;
                if (!IsFloor(cell, map, carpetDef)) continue;

                int id = regions.Count;
                var region = new List<IntVec3>();
                var queue = new Queue<IntVec3>();
                queue.Enqueue(cell);
                regionId[cell] = id;
                while (queue.Count > 0)
                {
                    var cur = queue.Dequeue();
                    region.Add(cur);
                    foreach (var d in Cardinals)
                    {
                        var n = cur + d;
                        if (!n.InBounds(map)) continue;
                        if (regionId.ContainsKey(n)) continue;
                        if (!IsFloor(n, map, carpetDef)) continue;
                        regionId[n] = id;
                        queue.Enqueue(n);
                    }
                }
                regions.Add(region);
            }

            
            foreach (var kvp in regionId)
            {
                var c = kvp.Key;
                int walkable = 0;
                foreach (var d in Cardinals)
                {
                    var n = c + d;
                    if (n.InBounds(map) && IsFloor(n, map, carpetDef)) walkable++;
                }
                if (walkable == 1) anchors.Add(c);
            }

            
            
            foreach (var region in regions)
            {
                if (region.Count < OneExitRoomMinSize || region.Count > OneExitRoomMaxSize) continue;

                var regionSet = new HashSet<IntVec3>(region);
                
                
                var exits = new List<IntVec3>();
                foreach (var c in region)
                {
                    bool touchesOutside = false;
                    foreach (var d in Cardinals)
                    {
                        var n = c + d;
                        if (!n.InBounds(map)) { touchesOutside = true; continue; }
                        if (!regionSet.Contains(n)) touchesOutside = true;
                    }
                    
                    
                    bool passageOut = false;
                    foreach (var d in Cardinals)
                    {
                        var n = c + d;
                        if (!n.InBounds(map)) continue;
                        if (!regionSet.Contains(n) && IsFloor(n, map, carpetDef)) passageOut = true;
                    }
                    if (passageOut) exits.Add(c);
                    else if (touchesOutside && exits.Count == 0 && region.Count <= OneExitRoomMinSize + 2)
                        exits.Add(c); 
                }

                if (exits.Count != 1) continue;
                var exit = exits[0];

                
                var dist = BFSFrom(exit, map, carpetDef);
                IntVec3 best = exit;
                int bestDist = -1;
                foreach (var c in region)
                {
                    if (!dist.TryGetValue(c, out var dd)) continue;
                    if (dd > bestDist) { bestDist = dd; best = c; }
                }
                if (bestDist >= 2) anchors.Add(best);
            }

            return anchors;
        }

        private static void MakeSpot(Map map, IntVec3 anchor, TerrainDef wetDef, TerrainDef carpetDef,
            ThingDef mushroomDef)
        {
            
            var wetCells = new List<IntVec3>();
            for (int dx = -WetRadius; dx <= WetRadius; dx++)
                for (int dz = -WetRadius; dz <= WetRadius; dz++)
                {
                    if (dx * dx + dz * dz > WetRadiusSq) continue; 
                    var c = anchor + new IntVec3(dx, 0, dz);
                    if (!c.InBounds(map)) continue;
                    if (!c.Standable(map)) continue;
                    if (map.terrainGrid.TerrainAt(c) != carpetDef) continue;
                    if (c.GetEdifice(map) != null) continue;
                    map.terrainGrid.SetTerrain(c, wetDef);
                    wetCells.Add(c);
                }

            
            if (mushroomDef != null)
            {
                var shuffled = InRandomOrder(wetCells);
                for (int i = 0; i < shuffled.Count; i++)
                {
                    if (i % 2 != 0) continue;
                    var c = shuffled[i];
                    if (c.GetEdifice(map) != null) continue;
                    if (c.GetPlant(map) != null) continue;
                    if (c.GetFirstItem(map) != null) continue;
                    var plant = GenSpawn.Spawn(mushroomDef, c, map, Rot4.North, WipeMode.Vanish) as Plant;
                    if (plant != null) plant.Growth = 1f;
                }
            }

            
            BreakLampsNear(map, anchor);
        }

        private static void BreakLampsNear(Map map, IntVec3 origin)
        {
            var lampDef = ThingDef.Named("YellowRooms_CeilingLamp");
            var brokenDef = ThingDef.Named("YellowRooms_CeilingLampBroken");
            if (lampDef == null || brokenDef == null) return;

            var reached = new HashSet<IntVec3>();
            var queue = new Queue<(IntVec3 cell, int dist)>();
            if (!origin.InBounds(map) || !origin.Standable(map)) return;
            reached.Add(origin); queue.Enqueue((origin, 0));
            while (queue.Count > 0)
            {
                var (cur, dist) = queue.Dequeue();
                if (dist >= LampFloodDepth) continue;
                foreach (var d in Cardinals)
                {
                    var n = cur + d;
                    if (!n.InBounds(map)) continue;
                    if (reached.Contains(n)) continue;
                    if (!n.Standable(map)) continue;
                    reached.Add(n);
                    queue.Enqueue((n, dist + 1));
                }
            }

            var lampsToBreak = new List<Thing>();
            foreach (var lamp in map.listerThings.ThingsOfDef(lampDef))
                if (reached.Contains(lamp.Position)) lampsToBreak.Add(lamp);

            foreach (var lamp in lampsToBreak)
            {
                var cell = lamp.Position;
                var rot = lamp.Rotation;
                lamp.DeSpawn();
                GenSpawn.Spawn(brokenDef, cell, map, rot, WipeMode.Vanish);
            }
        }

        
        private static bool IsFloor(IntVec3 c, Map map, TerrainDef carpetDef)
        {
            if (!c.InBounds(map)) return false;
            if (!c.Standable(map)) return false;
            var ed = c.GetEdifice(map);
            if (ed != null && ed.def.passability == Traversability.Impassable) return false;
            return map.terrainGrid.TerrainAt(c) == carpetDef;
        }

        
        private static Dictionary<IntVec3, int> BFSFrom(IntVec3 start, Map map, TerrainDef carpetDef)
        {
            var dist = new Dictionary<IntVec3, int>();
            var queue = new Queue<IntVec3>();
            dist[start] = 0; queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                int nd = dist[cur] + 1;
                foreach (var d in Cardinals)
                {
                    var n = cur + d;
                    if (!n.InBounds(map)) continue;
                    if (dist.ContainsKey(n)) continue;
                    if (!IsFloor(n, map, carpetDef)) continue;
                    dist[n] = nd;
                    queue.Enqueue(n);
                }
            }
            return dist;
        }

        private static int Chebyshev(IntVec3 a, IntVec3 b)
        {
            int dx = a.x - b.x; if (dx < 0) dx = -dx;
            int dz = a.z - b.z; if (dz < 0) dz = -dz;
            return dx > dz ? dx : dz;
        }

        private static List<T> InRandomOrder<T>(List<T> src)
        {
            var copy = new List<T>(src);
            for (int i = copy.Count - 1; i > 0; i--)
            {
                int j = Rand.RangeInclusive(0, i);
                (copy[i], copy[j]) = (copy[j], copy[i]);
            }
            return copy;
        }
    }
}
