using System.Collections.Generic;
using RimWorld;
using Verse;

namespace arsiy.Rooms.GenSteps
{
    
    
    
    
    
    
    public class GenStep_PoolRoomsFloor : GenStep
    {
        public override int SeedPart => 876543211;

        public override void Generate(Map map, GenStepParams parms)
        {
            var ceramic = DefDatabase<TerrainDef>.GetNamed("CeramicTile");
            if (ceramic == null) return;

            
            foreach (var cell in map.AllCells)
            {
                map.terrainGrid.SetUnderTerrain(cell, ceramic);
                map.terrainGrid.SetTerrain(cell, ceramic);
            }
        }
    }

    
    
    
    
    
    
    public class GenStep_PoolRoomsWalls : GenStep_YellowRoomsWalls
    {
        public override int SeedPart => 135792469;

        
        
        protected override int RoomMin => 4;
        protected override int RoomMax => 30;
        protected override int TargetRoomCount => 90;

        public override void Generate(Map map, GenStepParams parms)
        {
            
            base.Generate(map, parms);

            
            var concreteDef = ThingDef.Named("YellowRooms_ConcreteWall");
            var yellowDef = ThingDef.Named("YellowRooms_Wall");
            if (yellowDef == null || concreteDef == null) return;

            var yellows = map.listerThings.ThingsOfDef(yellowDef);
            for (int i = yellows.Count - 1; i >= 0; i--)
            {
                var w = yellows[i];
                var cell = w.Position;
                var rot = w.Rotation;
                w.DeSpawn();
                GenSpawn.Spawn(concreteDef, cell, map, rot, WipeMode.Vanish);
            }

            
            
            
            
            
            
            var rooms = new List<(int minX, int maxX, int minZ, int maxZ)>();
            int minRoomSide = RoomMin;
            foreach (var r in _generatedStructures)
            {
                int w = r.width, d = r.depth;
                if (w < minRoomSide || d < minRoomSide) continue;   
                rooms.Add((r.minX, r.maxX, r.minZ, r.maxZ));
            }
            PoolPoolPlacer.PlacePools(map, rooms);
        }
    }

    
    
    
    
    
    
    
    
    
    
    
    internal static class PoolPoolPlacer
    {
        
        private const float PoolChance = 0.55f;
        
        private const int MinRoomSideForPool = 4;
        
        private const float DeepFraction = 0.45f;

        public static void PlacePools(Map map, List<(int minX, int maxX, int minZ, int maxZ)> rooms)
        {
            var ceramic = DefDatabase<TerrainDef>.GetNamed("CeramicTile");
            var shallow = TerrainDefOf.WaterShallow;
            var deep = TerrainDefOf.WaterDeep;
            if (ceramic == null || shallow == null || deep == null) return;
            if (rooms == null || rooms.Count == 0) return;

            int placed = 0, scanned = 0;
            foreach (var room in rooms)
            {
                int w = room.maxX - room.minX + 1;
                int d = room.maxZ - room.minZ + 1;
                if (w < MinRoomSideForPool || d < MinRoomSideForPool) continue;
                scanned++;
                if (Rand.Value > PoolChance) continue;

                int variant = Rand.RangeInclusive(0, 2);
                bool ok;
                if (variant == 0)
                    ok = PoolAgainstWall(room, shallow, deep, map);
                else if (variant == 1)
                    ok = PoolFillRoom(room, shallow, deep, map);
                else
                    ok = PoolFillRoomWithPath(room, shallow, deep, ceramic, map);
                if (ok) placed++;
            }
            RoomsLog.Message($"[Rooms] Pool pools placed: {placed} (rooms scanned: {scanned}, total rooms: {rooms.Count})");
        }

        
        private static bool IsOpenFloor(IntVec3 c, Map map)
        {
            if (!c.InBounds(map)) return false;
            if (!c.Standable(map)) return false;
            var ed = c.GetEdifice(map);
            if (ed != null && ed.def.passability == Traversability.Impassable) return false;
            return true;
        }

        private static int RoomArea((int minX, int maxX, int minZ, int maxZ) r)
            => (r.maxX - r.minX + 1) * (r.maxZ - r.minZ + 1);

        
        private static int FillShallowRect(int x0, int z0, int w, int d,
            (int minX, int maxX, int minZ, int maxZ) room, TerrainDef shallow, Map map)
        {
            int count = 0;
            for (int x = x0; x < x0 + w; x++)
                for (int z = z0; z < z0 + d; z++)
                {
                    var c = new IntVec3(x, 0, z);
                    if (!IsOpenFloor(c, map)) continue;
                    map.terrainGrid.SetTerrain(c, shallow);
                    count++;
                }
            return count;
        }

        
        private static bool PoolAgainstWall((int minX, int maxX, int minZ, int maxZ) room,
            TerrainDef shallow, TerrainDef deep, Map map)
        {
            int roomW = room.maxX - room.minX + 1;
            int roomD = room.maxZ - room.minZ + 1;
            if (roomW < 4 || roomD < 3) return false;

            bool horizontal = roomW >= roomD;
            int poolLen = horizontal ? Rand.RangeInclusive(roomW / 2, roomW - 1) : Rand.RangeInclusive(roomD / 2, roomD - 1);
            int poolWid = Rand.RangeInclusive(2, System.Math.Max(2, (horizontal ? roomD : roomW) / 2));

            int startX, startZ, pw, pd;
            if (horizontal)
            {
                startX = room.minX + Rand.Range(0, roomW - poolLen + 1);
                startZ = room.minZ;   
                pw = poolLen; pd = poolWid;
            }
            else
            {
                startX = room.minX;   
                startZ = room.minZ + Rand.Range(0, roomD - poolLen + 1);
                pw = poolWid; pd = poolLen;
            }
            
            int filled = FillShallowRect(startX, startZ, pw, pd, room, shallow, map);
            if (filled == 0) return false;

            
            if (pw >= 3 && pd >= 3)
            {
                var deepCells = new HashSet<IntVec3>();
                for (int x = startX + 1; x < startX + pw - 1; x++)
                    for (int z = startZ + 1; z < startZ + pd - 1; z++)
                    {
                        var c = new IntVec3(x, 0, z);
                        if (IsOpenFloor(c, map)) deepCells.Add(c);
                    }
                CapAndApply(deepCells, RoomArea(room), deep, map);
            }
            return true;
        }

        
        private static bool PoolFillRoom((int minX, int maxX, int minZ, int maxZ) room,
            TerrainDef shallow, TerrainDef deep, Map map)
        {
            int roomW = room.maxX - room.minX + 1;
            int roomD = room.maxZ - room.minZ + 1;
            if (roomW < 4 || roomD < 4) return false;

            FillShallowRect(room.minX, room.minZ, roomW, roomD, room, shallow, map);

            int inset = System.Math.Max(1, System.Math.Min(roomW, roomD) / 4);
            var deepCells = new HashSet<IntVec3>();
            for (int x = room.minX + inset; x <= room.maxX - inset; x++)
                for (int z = room.minZ + inset; z <= room.maxZ - inset; z++)
                {
                    var c = new IntVec3(x, 0, z);
                    if (IsOpenFloor(c, map)) deepCells.Add(c);
                }
            CapAndApply(deepCells, RoomArea(room), deep, map);
            return true;
        }

        
        private static bool PoolFillRoomWithPath((int minX, int maxX, int minZ, int maxZ) room,
            TerrainDef shallow, TerrainDef deep, TerrainDef ceramic, Map map)
        {
            int roomW = room.maxX - room.minX + 1;
            int roomD = room.maxZ - room.minZ + 1;
            if (roomW < 4 || roomD < 4) return false;

            
            FillShallowRect(room.minX, room.minZ, roomW, roomD, room, shallow, map);

            
            bool verticalPath = roomW >= roomD;
            int pathThickness = Rand.RangeInclusive(1, 2);
            var pathCells = new HashSet<IntVec3>();
            if (verticalPath)
            {
                int px = room.minX + roomW / 2;
                for (int z = room.minZ; z <= room.maxZ; z++)
                    for (int t = 0; t < pathThickness; t++)
                        pathCells.Add(new IntVec3(System.Math.Min(px + t, room.maxX), 0, z));
            }
            else
            {
                int pz = room.minZ + roomD / 2;
                for (int x = room.minX; x <= room.maxX; x++)
                    for (int t = 0; t < pathThickness; t++)
                        pathCells.Add(new IntVec3(x, 0, System.Math.Min(pz + t, room.maxZ)));
            }
            foreach (var c in pathCells)
                if (IsOpenFloor(c, map))
                    map.terrainGrid.SetTerrain(c, ceramic);

            
            int inset = System.Math.Max(1, System.Math.Min(roomW, roomD) / 4);
            var deepCells = new HashSet<IntVec3>();
            for (int x = room.minX + inset; x <= room.maxX - inset; x++)
                for (int z = room.minZ + inset; z <= room.maxZ - inset; z++)
                {
                    var c = new IntVec3(x, 0, z);
                    if (pathCells.Contains(c)) continue;
                    if (IsOpenFloor(c, map)) deepCells.Add(c);
                }
            CapAndApply(deepCells, RoomArea(room), deep, map);
            return true;
        }

        
        private static void CapAndApply(HashSet<IntVec3> deepCells, int roomArea, TerrainDef deep, Map map)
        {
            if (deepCells.Count == 0) return;
            int maxDeep = System.Math.Max(1, (int)(roomArea * DeepFraction));
            if (deepCells.Count > maxDeep)
            {
                
                int cx = 0, cz = 0, n = 0;
                foreach (var c in deepCells) { cx += c.x; cz += c.z; n++; }
                cx /= n; cz /= n;
                var ordered = new List<IntVec3>(deepCells);
                ordered.Sort((a, b) =>
                    ((a.x - cx) * (a.x - cx) + (a.z - cz) * (a.z - cz))
                    .CompareTo((b.x - cx) * (b.x - cx) + (b.z - cz) * (b.z - cz)));
                deepCells = new HashSet<IntVec3>(ordered.GetRange(0, maxDeep));
            }
            foreach (var c in deepCells)
                if (c.InBounds(map))
                    map.terrainGrid.SetTerrain(c, deep);
        }
    }

    
    
    
    
    
    
    
    public class GenStep_ParkingLayout : GenStep
    {
        public override int SeedPart => 135792470;

        
        private const int GridSpacing = 9;

        
        
        private const float ClutterDensity = 0.036f;

        
        
        
        
        
        
        
        private const int SiteClearSize = 40;

        
        private static readonly string[] ClutterDefs =
        {
            "ChemfuelPoweredGenerator", "Battery", "ToolCabinet",
            "TubeTelevision", "StandingLamp", "SunLamp"
        };

        public override void Generate(Map map, GenStepParams parms)
        {
            var concrete = DefDatabase<TerrainDef>.GetNamed("Concrete");
            var ceramic = DefDatabase<TerrainDef>.GetNamed("CeramicTile");
            if (concrete == null || ceramic == null) return;

            
            
            
            
            
            
            CellRect? siteClear = null;
            if (map.info.parent is RimWorld.Planet.Site)
            {
                siteClear = CellRect.CenteredOn(map.Center, SiteClearSize, SiteClearSize).ClipInsideMap(map);
                RoomsLog.Message($"[Rooms] Parking site map: cleared central plaza {siteClear.Value} for site gensteps.");
            }

            
            foreach (var cell in map.AllCells)
            {
                map.terrainGrid.SetUnderTerrain(cell, ceramic);
                map.terrainGrid.SetTerrain(cell, concrete);
            }

            
            PlaceRoof(map);

            
            
            var columnDef = ThingDef.Named("Column");
            if (columnDef != null)
            {
                for (int x = GridSpacing / 2; x < map.Size.x; x += GridSpacing)
                    for (int z = GridSpacing / 2; z < map.Size.z; z += GridSpacing)
                    {
                        var cell = new IntVec3(x, 0, z);
                        if (siteClear.HasValue && siteClear.Value.Contains(cell)) continue;
                        if (CanPlaceAt(cell, map))
                            SpawnStuffBuilding(columnDef, ThingDefOf.Steel, cell, map);
                    }
            }

            
            var lampDef = ThingDef.Named("YellowRooms_CeilingLamp");
            if (lampDef != null)
            {
                for (int x = GridSpacing; x < map.Size.x; x += GridSpacing)
                    for (int z = GridSpacing; z < map.Size.z; z += GridSpacing)
                    {
                        var cell = new IntVec3(x, 0, z);
                        if (siteClear.HasValue && siteClear.Value.Contains(cell)) continue;
                        if (CanPlaceAt(cell, map))
                            GenSpawn.Spawn(lampDef, cell, map, Rot4.North, WipeMode.Vanish);
                    }
            }

            
            PlaceClutter(map, siteClear);
        }

        private static void PlaceRoof(Map map)
        {
            var roofDef = DefDatabase<RoofDef>.GetNamed("YellowRoomsRoof");
            if (roofDef == null) return;
            foreach (var c in map.AllCells)
                if (c.InBounds(map))
                    map.roofGrid.SetRoof(c, roofDef);
        }

        private static void PlaceClutter(Map map, CellRect? siteClear = null)
        {
            
            for (int x = 2; x < map.Size.x - 4; x += 5)
                for (int z = 2; z < map.Size.z - 4; z += 5)
                {
                    if (Rand.Value > ClutterDensity) continue;
                    var cell = new IntVec3(x, 0, z);
                    var defName = ClutterDefs[Rand.RangeInclusive(0, ClutterDefs.Length - 1)];
                    var def = ThingDef.Named(defName);
                    if (def == null) continue;

                    
                    
                    if (!CanPlaceFootprint(def, cell, Rot4.North, map, siteClear)) continue;

                    Thing thing;
                    if (def.MadeFromStuff)
                    {
                        var stuff = GenStuff.DefaultStuffFor(def) ?? ThingDefOf.Steel;
                        thing = ThingMaker.MakeThing(def, stuff);
                    }
                    else
                    {
                        thing = ThingMaker.MakeThing(def, null);
                    }
                    SetRandomBadQuality(thing);
                    GenSpawn.Spawn(thing, cell, map, Rot4.North, WipeMode.Vanish);
                    
                    TryFuelIfRefuelable(thing);
                }
        }

        
        private static void SpawnStuffBuilding(ThingDef def, ThingDef stuff, IntVec3 cell, Map map)
        {
            if (CanPlaceFootprint(def, cell, Rot4.North, map))
            {
                var thing = ThingMaker.MakeThing(def, stuff);
                SetRandomBadQuality(thing);
                GenSpawn.Spawn(thing, cell, map, Rot4.North, WipeMode.Vanish);
            }
        }

        
        private static void TryFuelIfRefuelable(Thing thing)
        {
            var comp = thing.TryGetComp<CompRefuelable>();
            if (comp == null) return;
            try { comp.Refuel(comp.Props.fuelCapacity); } catch {  }
        }

        private static void SetRandomBadQuality(Thing thing)
        {
            var compQuality = thing.TryGetComp<CompQuality>();
            if (compQuality == null) return;
            var q = Rand.Value < 0.5f ? QualityCategory.Awful : QualityCategory.Poor;
            compQuality.SetQuality(q, ArtGenerationContext.Outsider);
        }

        
        private static bool CanPlaceAt(IntVec3 c, Map map)
        {
            if (!c.InBounds(map)) return false;
            if (!c.Standable(map)) return false;
            if (c.GetEdifice(map) != null) return false;
            return true;
        }

        
        private static bool CanPlaceFootprint(ThingDef def, IntVec3 origin, Rot4 rot, Map map, CellRect? clearRect = null)
        {
            var size = def.Size;
            int w = size.x, d = size.z;
            if (rot.IsHorizontal) { int t = w; w = d; d = t; }
            for (int dx = 0; dx < w; dx++)
                for (int dz = 0; dz < d; dz++)
                {
                    var c = new IntVec3(origin.x + dx, 0, origin.z + dz);
                    if (!c.InBounds(map)) return false;
                    
                    if (clearRect.HasValue && clearRect.Value.Contains(c)) return false;
                    if (!c.Standable(map)) return false;
                    if (c.GetEdifice(map) != null) return false;
                    if (c.GetFirstItem(map) != null) return false;
                    if (c.GetFirstBuilding(map) != null) return false;
                }
            return true;
        }
    }
}
