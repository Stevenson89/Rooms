using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using arsiy.Rooms.Things;

namespace arsiy.Rooms.GenSteps
{
    
    
    
    
    
    
    
    
    
    
    
    
    public class GenStep_YellowRoomsEnvironment : GenStep
    {
        public override int SeedPart => 192837465;

        private const int LampSpacing = 5;
        
        private const int LampFillRadius = 6;
        
        private const int PitCount = 2;
        private const int HoleCount = 2;
        
        private const float FurnitureChance = 0.01f;

        
        private static readonly HashSet<string> NonRotatingFurniture =
            new HashSet<string> { "PlantPot", "Table2x2c" };

        private static readonly string[] FurnitureDefs =
        {
            "Stool", "DiningChair", "EndTable", "Dresser", "Bed", "Table1x2c", "Table2x2c", "PlantPot"
        };

        private static readonly IntVec3[] Cardinals =
        {
            new IntVec3(1, 0, 0), new IntVec3(-1, 0, 0),
            new IntVec3(0, 0, 1), new IntVec3(0, 0, -1)
        };

        public override void Generate(Map map, GenStepParams parms)
        {
            map.weatherManager.curWeather = Fog(map);

            if (map.GetComponent<YellowRoomsMapComponent>() == null)
                map.components.Add(new YellowRoomsMapComponent(map));

            
            
            
            
            YellowRoomsMapComponent.EnsureLayerReachability();

            PlaceLamps(map);
            PlaceWallOutlets(map);
            
            
            
            if (IsYellowRoomsBiome(map))
                PlacePitsAndHoles(map);
            
            
            if (IsYellowRoomsBiome(map))
                PlaceFurniture(map);

            
            
            
            
            EnsureAllRoomsConnected(map);

            
            
            
            PlaceCaravanPackingSpot(map);
        }

        
        
        private static bool IsYellowRoomsBiome(Map map)
        {
            var biome = map.Biome;
            return biome != null && biome.defName == "YellowRooms";
        }

        private static WeatherDef Fog(Map map) => WeatherDefOf.Fog;

        
        
        

        private void PlaceLamps(Map map)
        {
            var lampDef = ThingDef.Named("YellowRooms_CeilingLamp");
            if (lampDef == null) return;

            
            int sx = map.Size.x, sz = map.Size.z;
            for (int x = LampSpacing / 2; x < sx; x += LampSpacing)
                for (int z = LampSpacing / 2; z < sz; z += LampSpacing)
                {
                    var cell = new IntVec3(x, 0, z);
                    if (CanPlaceLamp(cell, map))
                        GenSpawn.Spawn(lampDef, cell, map, Rot4.North, WipeMode.Vanish);
                }

            
            FillUnlitCells(map, lampDef);
        }

        
        
        
        
        
        
        
        private void FillUnlitCells(Map map, ThingDef lampDef)
        {
            var covered = new HashSet<IntVec3>();

            
            foreach (var lamp in map.listerThings.ThingsOfDef(lampDef))
                FloodAdd(covered, lamp.Position, map, LampFillRadius);

            int sx = map.Size.x, sz = map.Size.z;
            for (int x = 1; x < sx - 1; x++)
                for (int z = 1; z < sz - 1; z++)
                {
                    var cell = new IntVec3(x, 0, z);
                    
                    if (covered.Contains(cell)) continue;
                    if (!cell.Standable(map)) continue;
                    if (!CanPlaceLamp(cell, map)) continue;

                    var target = NudgeOffWall(cell, map);
                    if (!CanPlaceLamp(target, map)) target = cell;
                    if (!CanPlaceLamp(target, map)) continue;
                    
                    if (covered.Contains(target)) continue;

                    GenSpawn.Spawn(lampDef, target, map, Rot4.North, WipeMode.Vanish);
                    
                    
                    FloodAdd(covered, target, map, LampFillRadius);
                }
        }

        
        
        private static void FloodAdd(HashSet<IntVec3> set, IntVec3 origin, Map map, int maxDist)
        {
            if (!origin.InBounds(map)) return;
            var localDist = new Dictionary<IntVec3, int>();
            var queue = new Queue<IntVec3>();
            localDist[origin] = 0;
            queue.Enqueue(origin);
            set.Add(origin);
            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                int nd = localDist[cur] + 1;
                if (nd > maxDist) continue;
                foreach (var d in Cardinals)
                {
                    var nxt = cur + d;
                    if (!nxt.InBounds(map)) continue;
                    if (!nxt.Standable(map)) continue;
                    if (localDist.ContainsKey(nxt)) continue;
                    localDist[nxt] = nd;
                    set.Add(nxt);
                    queue.Enqueue(nxt);
                }
            }
        }

        
        
        
        private static IntVec3 NudgeOffWall(IntVec3 c, Map map)
        {
            foreach (var d in Cardinals)
            {
                if (IsWall(c + d, map))
                {
                    var away = c - d;
                    if (away.InBounds(map) && away.Standable(map) && away.GetEdifice(map) == null)
                        return away;
                }
            }
            return c;
        }

        
        
        

        
        
        
        
        private void PlaceWallOutlets(Map map)
        {
            if (!IsYellowRoomsBiome(map)) return;

            var outletDef = ThingDef.Named("YellowRooms_WallOutlet");
            if (outletDef == null) return;

            var wallDef = ThingDef.Named("YellowRooms_Wall");
            if (wallDef == null) return;

            var validPairs = new List<(IntVec3 wall, IntVec3 floor, Rot4 rot)>();

            foreach (var cell in map.AllCells)
            {
                if (!cell.InBounds(map)) continue;
                var ed = cell.GetEdifice(map);
                if (ed == null || ed.def != wallDef) continue;

                foreach (var d in Cardinals)
                {
                    var floorCell = cell + d;
                    if (!floorCell.InBounds(map)) continue;
                    if (!floorCell.Standable(map)) continue;
                    if (floorCell.GetEdifice(map) != null) continue;
                    if (floorCell.GetFirstThing(map, outletDef) != null) continue;

                    var rot = DirectionToRot(d);
                    validPairs.Add((cell, floorCell, rot));
                }
            }

            var shuffled = InRandomOrder(validPairs);
            int count = Rand.RangeInclusive(5, 10);
            int placed = 0;

            
            var occupied = new HashSet<IntVec3>();
            foreach (var pair in shuffled)
            {
                if (placed >= count) break;
                if (occupied.Contains(pair.floor)) continue;
                if (occupied.Contains(pair.wall)) continue;

                GenSpawn.Spawn(outletDef, pair.floor, map, pair.rot, WipeMode.Vanish);
                occupied.Add(pair.floor);
                placed++;
            }

            RoomsLog.Message($"[Rooms] Placed {placed}/{count} wall outlet(s)");
        }

        
        private static Rot4 DirectionToRot(IntVec3 dir)
        {
            if (dir.x > 0) return Rot4.East;
            if (dir.x < 0) return Rot4.West;
            if (dir.z > 0) return Rot4.North;
            return Rot4.South;
        }

        private static bool IsWall(IntVec3 c, Map map)
        {
            if (!c.InBounds(map)) return true;
            var ed = c.GetEdifice(map);
            return ed != null && ed.def.passability == Traversability.Impassable;
        }

        private static bool CanPlaceLamp(IntVec3 c, Map map)
        {
            if (!c.InBounds(map)) return false;
            if (HasLampAt(c, map)) return false;
            return true;
        }

        private static bool HasLampAt(IntVec3 c, Map map)
        {
            var lamp = ThingDef.Named("YellowRooms_CeilingLamp");
            var broken = ThingDef.Named("YellowRooms_CeilingLampBroken");
            return (lamp != null && c.GetFirstThing(map, lamp) != null)
                || (broken != null && c.GetFirstThing(map, broken) != null);
        }

        
        
        

        private void PlacePitsAndHoles(Map map)
        {
            var pitDef = ThingDef.Named("YellowRooms_Pit");
            var holeDef = ThingDef.Named("YellowRooms_CeilingHole");
            if (pitDef == null && holeDef == null) return;

            var candidates = new List<IntVec3>();
            foreach (var origin in CandidateOrigins(map, step: 1))
                if (Fits2x2(origin, map))
                    candidates.Add(origin);

            var shuffled = InRandomOrder(candidates);

            int pitsPlaced = 0, holesPlaced = 0;
            foreach (var origin in shuffled)
            {
                if (pitsPlaced >= PitCount && holesPlaced >= HoleCount)
                    break;

                if (pitsPlaced < PitCount && pitDef != null)
                {
                    Spawn2x2(pitDef, origin, map);
                    pitsPlaced++;
                }
                else if (holesPlaced < HoleCount && holeDef != null)
                {
                    Spawn2x2(holeDef, origin, map);
                    holesPlaced++;
                }
            }

            RoomsLog.Message($"[Rooms] Placed pits={pitsPlaced}/{PitCount} holes={holesPlaced}/{HoleCount}");
        }

        
        
        private static bool Fits2x2(IntVec3 origin, Map map)
        {
            for (int dx = 0; dx < 2; dx++)
                for (int dz = 0; dz < 2; dz++)
                {
                    var c = new IntVec3(origin.x + dx, 0, origin.z + dz);
                    if (!c.InBounds(map)) return false;
                    if (!c.Standable(map)) return false;
                    if (c.GetEdifice(map) != null) return false;
                    if (HasLampAt(c, map)) return false;
                }
            return true;
        }

        private static void Spawn2x2(ThingDef def, IntVec3 origin, Map map)
            => GenSpawn.Spawn(def, origin, map, Rot4.North, WipeMode.Vanish);

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

        
        
        

        private void PlaceFurniture(Map map)
        {
            foreach (var cell in AllFloorCells(map))
            {
                if (Rand.Value > FurnitureChance) continue;

                var defName = FurnitureDefs[Rand.RangeInclusive(0, FurnitureDefs.Length - 1)];
                var def = ThingDef.Named(defName);
                if (def == null) continue;

                
                var rot = NonRotatingFurniture.Contains(defName) ? Rot4.North : Rot4.Random;

                
                
                if (!CanPlaceFurniture(def, cell, rot, map)) continue;

                var wood = GenStuff.DefaultStuffFor(def) ?? ThingDefOf.WoodLog;
                var thing = ThingMaker.MakeThing(def, wood);
                SetRandomBadQuality(thing);
                GenSpawn.Spawn(thing, cell, map, rot, WipeMode.Vanish);

                
                
                WetCarpetUnderFurniture(thing, cell, rot, map);
            }
        }

        
        
        private static bool CanPlaceFurniture(ThingDef def, IntVec3 origin, Rot4 rot, Map map)
        {
            foreach (var c in OccupiedCells(def, origin, rot))
            {
                if (!c.InBounds(map)) return false;
                if (!c.Standable(map)) return false;
                if (c.GetEdifice(map) != null) return false;
                if (HasLampAt(c, map)) return false;
                
                if (c.GetFirstItem(map) != null) return false;
                if (c.GetFirstBuilding(map) != null) return false;
            }
            return true;
        }

        
        private static IEnumerable<IntVec3> OccupiedCells(ThingDef def, IntVec3 origin, Rot4 rot)
        {
            var size = def.Size;
            
            int w = size.x;
            int d = size.z;
            if (rot.IsHorizontal)
            {
                int t = w; w = d; d = t;
            }
            for (int dx = 0; dx < w; dx++)
                for (int dz = 0; dz < d; dz++)
                    yield return new IntVec3(origin.x + dx, 0, origin.z + dz);
        }

        
        
        

        private static IEnumerable<IntVec3> AllFloorCells(Map map)
        {
            foreach (var c in map.AllCells)
                if (c.Standable(map) && c.GetEdifice(map) == null)
                    yield return c;
        }

        private static IEnumerable<IntVec3> CandidateOrigins(Map map, int step)
        {
            int sx = map.Size.x, sz = map.Size.z;
            for (int x = 1; x < sx - 2; x += step)
                for (int z = 1; z < sz - 2; z += step)
                    yield return new IntVec3(x, 0, z);
        }

        private static void PlaceCaravanPackingSpot(Map map)
        {
            var spotDef = ThingDef.Named("CaravanPackingSpot");
            if (spotDef == null) return;

            var center = map.Center;
            for (int r = 0; r <= 5; r++)
            {
                foreach (var cell in GenRadial.RadialCellsAround(center, r, useCenter: true))
                {
                    if (!cell.InBounds(map)) continue;
                    if (!cell.Standable(map)) continue;
                    if (cell.GetEdifice(map) != null) continue;
                    if (cell.GetFirstThing(map, spotDef) != null) continue;
                    GenSpawn.Spawn(spotDef, cell, map, Rot4.North, WipeMode.Vanish);
                    return;
                }
            }
        }

        private static void WetCarpetUnderFurniture(Thing thing, IntVec3 origin, Rot4 rot, Map map)
        {
            var carpetDef = DefDatabase<TerrainDef>.GetNamedSilentFail("CarpetYellow");
            var wetDef = DefDatabase<TerrainDef>.GetNamedSilentFail("CarpetYellowWet");
            if (carpetDef == null || wetDef == null) return;

            var occupied = OccupiedCells(thing.def, origin, rot);
            foreach (var c in occupied)
            {
                if (!c.InBounds(map)) continue;
                if (map.terrainGrid.TerrainAt(c) != carpetDef) continue;
                var adjacentToWet = false;
                foreach (var d in Cardinals)
                {
                    var n = c + d;
                    if (!n.InBounds(map)) continue;
                    if (map.terrainGrid.TerrainAt(n) == wetDef)
                    {
                        adjacentToWet = true;
                        break;
                    }
                }
                if (adjacentToWet)
                    map.terrainGrid.SetTerrain(c, wetDef);
            }
        }

        private static void SetRandomBadQuality(Thing thing)
        {
            var compQuality = thing.TryGetComp<CompQuality>();
            if (compQuality == null) return;
            var q = Rand.Value < 0.5f ? QualityCategory.Awful : QualityCategory.Poor;
            compQuality.SetQuality(q, ArtGenerationContext.Outsider);
        }

        
        
        

        private static readonly IntVec3[] ConnCardinal =
        {
            new IntVec3(1, 0, 0), new IntVec3(-1, 0, 0),
            new IntVec3(0, 0, 1), new IntVec3(0, 0, -1)
        };

        
        
        
        
        
        
        
        
        
        
        private static void EnsureAllRoomsConnected(Map map)
        {
            var deep = TerrainDefOf.WaterDeep;
            var shallow = TerrainDefOf.WaterShallow;
            var ceramic = DefDatabase<TerrainDef>.GetNamedSilentFail("CeramicTile");
            var pitDef = ThingDef.Named("YellowRooms_Pit");
            var wallDefs = new List<ThingDef>();
            var yw = ThingDef.Named("YellowRooms_Wall"); if (yw != null) wallDefs.Add(yw);
            var cw = ThingDef.Named("YellowRooms_ConcreteWall"); if (cw != null) wallDefs.Add(cw);

            int iter = 0;
            while (iter++ < 128)
            {
                var groups = ConnFloorGroups(map);
                if (groups.Count <= 1)
                {
                    if (iter > 1) RoomsLog.Message($"[Rooms] Connectivity: all rooms joined after {iter - 1} link(s)");
                    return;
                }
                
                groups.Sort((a, b) => b.Count.CompareTo(a.Count));
                var main = groups[0];
                var mainSet = new HashSet<IntVec3>(main);

                bool linked = false;
                for (int i = 1; i < groups.Count && !linked; i++)
                {
                    if (TryLinkIslandDijkstra(groups[i], mainSet, map, deep, shallow, ceramic, pitDef, wallDefs))
                        linked = true;
                }
                if (!linked)
                {
                    
                    
                    if (!RemoveOnePitBorderingIsland(groups, map, pitDef))
                    {
                        RoomsLog.Warning($"[Rooms] Connectivity: could not link an island of size {(groups.Count > 1 ? groups[1].Count : 0)}; giving up");
                        return;
                    }
                }
            }
        }

        
        
        
        private static List<List<IntVec3>> ConnFloorGroups(Map map)
        {
            var groups = new List<List<IntVec3>>();
            var visited = new HashSet<IntVec3>();
            var deep = TerrainDefOf.WaterDeep;
            foreach (var cell in map.AllCells)
            {
                if (visited.Contains(cell)) continue;
                if (!IsOpenForPath(cell, map, deep)) continue;

                var grp = new List<IntVec3>();
                var queue = new Queue<IntVec3>();
                queue.Enqueue(cell);
                visited.Add(cell);
                while (queue.Count > 0)
                {
                    var cur = queue.Dequeue();
                    grp.Add(cur);
                    foreach (var d in ConnCardinal)
                    {
                        var n = cur + d;
                        if (!n.InBounds(map)) continue;
                        if (visited.Contains(n)) continue;
                        if (!IsOpenForPath(n, map, deep)) continue;
                        visited.Add(n);
                        queue.Enqueue(n);
                    }
                }
                if (grp.Count > 0)
                    groups.Add(grp);
            }
            return groups;
        }

        private static bool IsOpenForPath(IntVec3 c, Map map, TerrainDef deep)
        {
            if (!c.InBounds(map)) return false;
            if (!c.Standable(map)) return false;
            
            if (deep != null && map.terrainGrid.TerrainAt(c) == deep) return false;
            var ed = c.GetEdifice(map);
            if (ed != null && ed.def.passability == Traversability.Impassable) return false;
            return true;
        }

        
        
        
        
        
        
        
        
        private static bool TryLinkIslandDijkstra(List<IntVec3> island, HashSet<IntVec3> mainSet,
            Map map, TerrainDef deep, TerrainDef shallow, TerrainDef ceramic, ThingDef pitDef,
            List<ThingDef> wallDefs)
        {
            
            int CellCost(IntVec3 c)
            {
                if (!c.InBounds(map)) return -1;
                
                if (pitDef != null && c.GetFirstThing(map, pitDef) != null) return -1;
                var ed = c.GetEdifice(map);
                if (ed != null && wallDefs.Contains(ed.def)) return 30; 
                if (deep != null && map.terrainGrid.TerrainAt(c) == deep) return 25; 
                if (ed != null && ed.def.passability == Traversability.Impassable) return -1; 
                
                return 1;
            }

            var dist = new Dictionary<IntVec3, int>();
            var cameFrom = new Dictionary<IntVec3, IntVec3>();
            
            
            var pq = new SortedSet<(int cost, int idx, IntVec3 cell)>(
                Comparer<(int cost, int idx, IntVec3 cell)>.Create((a, b) =>
                {
                    int c = a.cost.CompareTo(b.cost);
                    if (c != 0) return c;
                    return a.idx.CompareTo(b.idx);
                }));

            
            var ci = map.cellIndices;
            foreach (var s in island)
            {
                if (!s.InBounds(map)) continue;
                dist[s] = 0;
                pq.Add((0, ci.CellToIndex(s), s));
            }

            IntVec3 goal = IntVec3.Invalid;
            int budget = 120; 
            while (pq.Count > 0)
            {
                var (curCost, _, cur) = pq.Min;
                pq.Remove(pq.Min);
                if (curCost > budget) break;
                if (dist.TryGetValue(cur, out var rec) && curCost > rec) continue;

                
                if (mainSet.Contains(cur) && !island.Contains(cur))
                {
                    goal = cur;
                    break;
                }

                foreach (var d in ConnCardinal)
                {
                    var n = cur + d;
                    int nc = CellCost(n);
                    if (nc < 0) continue;
                    int nd = curCost + nc;
                    if (nd > budget) continue;
                    if (!dist.TryGetValue(n, out var old) || nd < old)
                    {
                        if (dist.ContainsKey(n)) pq.Remove((old, ci.CellToIndex(n), n));
                        dist[n] = nd;
                        cameFrom[n] = cur;
                        pq.Add((nd, ci.CellToIndex(n), n));
                    }
                }
            }

            if (!goal.IsValid) return false;

            
            
            var path = new List<IntVec3>();
            var step = goal;
            while (cameFrom.TryGetValue(step, out var prev))
            {
                path.Add(step);
                step = prev;
            }
            

            bool clearedAny = false;
            foreach (var c in path)
            {
                if (island.Contains(c) || mainSet.Contains(c)) continue;
                if (ClearBarrier(c, map, deep, shallow, ceramic, pitDef, wallDefs))
                    clearedAny = true;
            }

            
            if (clearedAny || path.Count > 0)
            {
                RoomsLog.Message($"[Rooms] Connectivity: linked island (size {island.Count}) via {path.Count}-cell route (cleared {path.Count} barriers max)");
                return true;
            }
            return false;
        }

        
        
        
        
        
        
        
        private static bool ClearBarrier(IntVec3 c, Map map, TerrainDef deep, TerrainDef shallow,
            TerrainDef ceramic, ThingDef pitDef, List<ThingDef> wallDefs)
        {
            if (!c.InBounds(map)) return false;

            
            if (deep != null && map.terrainGrid.TerrainAt(c) == deep)
            {
                if (shallow != null) map.terrainGrid.SetTerrain(c, shallow);
                return true;
            }

            
            var ed = c.GetEdifice(map);
            if (ed != null && wallDefs.Contains(ed.def))
            {
                ed.DeSpawn(DestroyMode.Vanish);
                if (c.GetEdifice(map) == null)
                {
                    
                    
                    var t = map.terrainGrid.TerrainAt(c);
                    if (ceramic != null && t == null)
                        map.terrainGrid.SetTerrain(c, ceramic);
                    return true;
                }
            }

            
            
            return true;
        }

        
        
        
        
        
        
        private static bool RemoveOnePitBorderingIsland(List<List<IntVec3>> groups, Map map, ThingDef pitDef)
        {
            if (pitDef == null || groups.Count <= 1) return false;
            
            for (int i = 1; i < groups.Count; i++)
            {
                foreach (var cell in groups[i])
                {
                    foreach (var d in ConnCardinal)
                    {
                        var n = cell + d;
                        if (!n.InBounds(map)) continue;
                        var pit = n.GetFirstThing(map, pitDef);
                        if (pit != null)
                        {
                            pit.DeSpawn(DestroyMode.Vanish);
                            RoomsLog.Message($"[Rooms] Connectivity: pit-ring fallback — removed pit at {n} bordering island of size {groups[i].Count}");
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
