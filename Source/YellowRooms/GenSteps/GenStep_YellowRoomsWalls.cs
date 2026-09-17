using System.Collections.Generic;
using RimWorld;
using Verse;

namespace arsiy.Rooms.GenSteps
{
    public class GenStep_YellowRoomsWalls : GenStep
    {
        public override int SeedPart => 135792468;

        
        
        protected virtual int RoomMin => 2;
        protected virtual int RoomMax => 15;
        private const int CorridorWidthMin = 1;
        private const int CorridorWidthMax = 3;
        private const int CorridorSegmentMin = 2;
        private const int CorridorSegmentMax = 12;
        protected virtual int TargetRoomCount => 60;
        private const int GrowthAttempts = 260;
        private const int LargeWallBlockMin = 10;
        private const int LargeWallBlockMax = 16;
        private const int LargeWallBlockAttempts = 400;

        private ThingDef _wallDef;
        private Map _map;
        private int _mapSizeX;
        private int _mapSizeZ;

        private readonly HashSet<IntVec3> _floorCells = new HashSet<IntVec3>();
        private readonly HashSet<IntVec3> _wallCells = new HashSet<IntVec3>();

        public override void Generate(Map map, GenStepParams parms)
        {
            _floorCells.Clear();
            _wallCells.Clear();
            _generatedStructures.Clear();

            _wallDef = DefDatabase<ThingDef>.GetNamed("YellowRooms_Wall");
            if (_wallDef == null)
            {
                RoomsLog.Error("[Rooms] YellowRooms_Wall def not found; skipping room generation.");
                return;
            }
            _map = map;
            _mapSizeX = map.Size.x;
            _mapSizeZ = map.Size.z;

            var structures = new List<Rect>();

            
            
            
            int seedCount = Rand.Value < 0.6f ? 1 : 2;
            for (int s = 0; s < seedCount; s++)
            {
                var seed = MakeRoomRectNear(RandomSeedPoint());
                if (Commit(seed))
                    structures.Add(seed);
            }
            
            if (structures.Count == 0)
            {
                var seed = MakeRoomRectNear(new IntVec2(_mapSizeX / 2, _mapSizeZ / 2));
                if (Commit(seed))
                    structures.Add(seed);
            }

            
            
            
            
            if (Rand.Value < 0.5f)
            {
                var big = MakeBigHallRect();
                if (Commit(big))
                {
                    structures.Add(big);
                    RoomsLog.Message($"[Rooms] Big hall placed at ({big.minX},{big.minZ}) size {big.width}x{big.depth}");
                }
            }

            int grown = 0;
            for (int attempt = 0; attempt < GrowthAttempts && grown < TargetRoomCount; attempt++)
            {
                if (structures.Count == 0) break;
                var from = structures[Rand.RangeInclusive(0, structures.Count - 1)];

                if (!TryPickEdge(from, out IntVec2 edge, out IntVec2 dir))
                    continue;

                Rect? built;
                switch (Rand.RangeInclusive(0, 2))
                {
                    default:
                    case 0: built = TryGrowRoom(edge, dir); break;
                    case 1: built = TryGrowTrunkCorridor(edge, dir, structures); break;
                    case 2: built = TryGrowWindingCorridor(edge, dir); break;
                }
                if (built.HasValue)
                {
                    structures.Add(built.Value);
                    grown++;
                }
            }

            FillEmptyAreas(structures);

            _wallCells.RemoveWhere(c => _floorCells.Contains(c));

            var allWalls = new HashSet<IntVec3>(_wallCells);
            foreach (var c in _map.AllCells)
                if (!_floorCells.Contains(c))
                    allWalls.Add(c);

            RoomsLog.Message($"[Rooms] Initial floor cells: {_floorCells.Count}, wall cells: {allWalls.Count}");

            RemoveSmallWallClusters(allWalls);
            RoomsLog.Message($"[Rooms] After RemoveSmallWallClusters: walls={allWalls.Count}, floors={_floorCells.Count}");

            CreateMapEdgeExits(allWalls);
            RoomsLog.Message($"[Rooms] After CreateMapEdgeExits: walls={allWalls.Count}");

            CarveLargeWallBlocks(allWalls, structures);
            RoomsLog.Message($"[Rooms] After CarveLargeWallBlocks: walls={allWalls.Count}, floors={_floorCells.Count}");

            EnsureConnectivityToEdge(allWalls);

            RemoveSmallWallClusters(allWalls);
            int floorBeforeFinal = _floorCells.Count;
            RoomsLog.Message($"[Rooms] After connectivity: walls={allWalls.Count}, floors={_floorCells.Count}");

            EnsureFinalExitExists(allWalls);

            EnsureExitsOnMultipleSides(allWalls);

            EnsureAllFloorGroupsConnected(allWalls);

            foreach (var c in allWalls)
                if (c.InBounds(_map) && c.GetEdifice(_map) == null)
                    GenSpawn.Spawn(_wallDef, c, _map, WipeMode.Vanish);

            RoomsLog.Message($"[Rooms] Walls spawned. Total floor cells: {_floorCells.Count}");

            foreach (var c in _floorCells)
            {
                if (c.InBounds(_map) && c.GetEdifice(_map) == null)
                {
                    MapGenerator.PlayerStartSpot = c;
                    break;
                }
            }

            
            
            _generatedStructures.Clear();
            _generatedStructures.AddRange(structures);

            PlaceRoof();

            _floorCells.Clear();
            _wallCells.Clear();
        }

        
        
        
        
        
        private void PlaceRoof()
        {
            var roofDef = DefDatabase<RoofDef>.GetNamed("YellowRoomsRoof");
            if (roofDef == null) return;
            foreach (var c in _map.AllCells)
            {
                if (!c.InBounds(_map)) continue;
                _map.roofGrid.SetRoof(c, roofDef);
            }
        }

        private void RemoveSmallWallClusters(HashSet<IntVec3> wallSet)
        {
            var visited = new HashSet<IntVec3>();
            var toRemove = new List<IntVec3>();

            foreach (var cell in wallSet)
            {
                if (visited.Contains(cell)) continue;

                var cluster = new List<IntVec3>();
                var queue = new Queue<IntVec3>();
                queue.Enqueue(cell);
                visited.Add(cell);

                while (queue.Count > 0)
                {
                    var cur = queue.Dequeue();
                    cluster.Add(cur);

                    foreach (var adj in GenAdj.AdjacentCells)
                    {
                        var next = cur + adj;
                        if (next.InBounds(_map) && wallSet.Contains(next) && !visited.Contains(next))
                        {
                            visited.Add(next);
                            queue.Enqueue(next);
                        }
                    }
                }

                if (cluster.Count <= 6)
                    toRemove.AddRange(cluster);
            }

            foreach (var c in toRemove)
            {
                wallSet.Remove(c);
                _floorCells.Add(c);
            }
        }

        private void FillEmptyAreas(List<Rect> structures)
        {
            var emptyAreas = new List<List<IntVec3>>();
            var visited = new HashSet<IntVec3>();

            foreach (var cell in _map.AllCells)
            {
                if (visited.Contains(cell) || _floorCells.Contains(cell)) continue;

                var area = new List<IntVec3>();
                var queue = new Queue<IntVec3>();
                queue.Enqueue(cell);
                visited.Add(cell);

                while (queue.Count > 0)
                {
                    var cur = queue.Dequeue();
                    area.Add(cur);

                    foreach (var adj in GenAdj.AdjacentCells)
                    {
                        var next = cur + adj;
                        if (next.InBounds(_map) && !visited.Contains(next) && !_floorCells.Contains(next))
                        {
                            visited.Add(next);
                            queue.Enqueue(next);
                        }
                    }
                }

                if (area.Count >= RoomMin * RoomMin)
                    emptyAreas.Add(area);
            }

            foreach (var area in emptyAreas)
            {
                int minX = int.MaxValue, minZ = int.MaxValue, maxX = 0, maxZ = 0;
                foreach (var c in area)
                {
                    if (c.x < minX) minX = c.x;
                    if (c.z < minZ) minZ = c.z;
                    if (c.x > maxX) maxX = c.x;
                    if (c.z > maxZ) maxZ = c.z;
                }

                int w = maxX - minX + 1;
                int d = maxZ - minZ + 1;

                
                
                int maxAreaSide = RoomMax * 2;
                if (w > maxAreaSide || d > maxAreaSide) continue;
                if (w < RoomMin + 2 || d < RoomMin + 2) continue;

                int roll = Rand.RangeInclusive(0, 2);
                if (roll == 0)
                    FillAreaWithConnectedSmallRooms(minX, minZ, w, d, structures);
                else if (roll == 1)
                    FillAreaWithBigRoomAndCorridor(minX, minZ, w, d, structures);
                else
                    FillAreaWithNormalRooms(minX, minZ, w, d, structures);
            }
        }

        
        
        
        private void CarveLargeWallBlocks(HashSet<IntVec3> allWalls, List<Rect> structures)
        {
            var used = new HashSet<IntVec3>();
            int carved = 0;

            for (int attempt = 0; attempt < LargeWallBlockAttempts; attempt++)
            {
                int w = Rand.RangeInclusive(LargeWallBlockMin, LargeWallBlockMax);
                int d = Rand.RangeInclusive(LargeWallBlockMin, LargeWallBlockMax);
                int maxX0 = _mapSizeX - 1 - w;
                int maxZ0 = _mapSizeZ - 1 - d;
                if (maxX0 < 2 || maxZ0 < 2) continue;
                int x0 = Rand.RangeInclusive(1, maxX0);
                int z0 = Rand.RangeInclusive(1, maxZ0);

                
                int wallCount = 0, usedCount = 0;
                for (int x = x0; x < x0 + w; x++)
                    for (int z = z0; z < z0 + d; z++)
                    {
                        var c = new IntVec3(x, 0, z);
                        if (used.Contains(c)) usedCount++;
                        if (allWalls.Contains(c)) wallCount++;
                    }
                int area = w * d;
                if (wallCount < area * 0.85) continue;   
                if (usedCount > area * 0.1) continue;    

                if (TryCarveRoomInBlock(x0, z0, w, d, allWalls, structures, used))
                    carved++;
            }
            RoomsLog.Message($"[Rooms] CarveLargeWallBlocks carved={carved}");
        }

        private bool TryCarveRoomInBlock(int bx, int bz, int bw, int bd,
            HashSet<IntVec3> allWalls, List<Rect> structures, HashSet<IntVec3> used)
        {
            var carved = new List<Rect>();

            if (Rand.Value < 0.5f)
            {
                var r = CarveRoomInsideBlock(bx, bz, bw, bd, allWalls, border: 2, minSide: RoomMin, maxSide: RoomMax);
                if (r.HasValue) carved.Add(r.Value);
            }
            else
            {
                
                int roomCount = Rand.RangeInclusive(2, 3);
                for (int i = 0; i < roomCount; i++)
                {
                    var r = CarveRoomInsideBlock(bx, bz, bw, bd, allWalls, border: 1, minSide: RoomMin, maxSide: 6);
                    if (r.HasValue) carved.Add(r.Value);
                }
            }

            if (carved.Count == 0) return false;

            foreach (var r in carved)
                structures.Add(r);

            
            for (int x = bx; x < bx + bw; x++)
                for (int z = bz; z < bz + bd; z++)
                    used.Add(new IntVec3(x, 0, z));

            
            foreach (var r in carved)
                ConnectRoomToNetwork(r, allWalls);

            return true;
        }

        private Rect? CarveRoomInsideBlock(int bx, int bz, int bw, int bd,
            HashSet<IntVec3> allWalls, int border, int minSide, int maxSide)
        {
            int availW = bw - border * 2;
            int availD = bd - border * 2;
            if (availW < minSide || availD < minSide) return null;

            int rw = Rand.RangeInclusive(minSide, System.Math.Min(maxSide, availW));
            int rd = Rand.RangeInclusive(minSide, System.Math.Min(maxSide, availD));
            int rx = bx + border + Rand.Range(0, availW - rw + 1);
            int rz = bz + border + Rand.Range(0, availD - rd + 1);

            var rect = new Rect { minX = rx, minZ = rz, width = rw, depth = rd };
            CarveRectFloor(rect, allWalls);
            return rect;
        }

        
        
        private void CarveRectFloor(Rect r, HashSet<IntVec3> allWalls)
        {
            for (int x = r.minX; x <= r.maxX; x++)
                for (int z = r.minZ; z <= r.maxZ; z++)
                    CarveFloorCell(x, z, allWalls);
        }

        private void CarveFloorCell(int x, int z, HashSet<IntVec3> allWalls)
        {
            var cell = new IntVec3(x, 0, z);
            if (!cell.InBounds(_map)) return;
            allWalls.Remove(cell);
            _floorCells.Add(cell);
        }

        
        
        private void ConnectRoomToNetwork(Rect room, HashSet<IntVec3> allWalls)
        {
            int ax = room.minX + room.width / 2;
            int az = room.minZ + room.depth / 2;

            IntVec3 target = FindNearestNetworkCell(room);
            if (!target.IsValid) return;
            int bx = target.x, bz = target.z;

            CarveAxisLine(ax, az, bx, az, allWalls); 
            CarveAxisLine(bx, az, bx, bz, allWalls); 
        }

        private IntVec3 FindNearestNetworkCell(Rect room)
        {
            int cx = room.minX + room.width / 2;
            int cz = room.minZ + room.depth / 2;
            IntVec3 best = IntVec3.Invalid;
            int bestDist = int.MaxValue;
            foreach (var f in _floorCells)
            {
                
                if (f.x >= room.minX && f.x <= room.maxX && f.z >= room.minZ && f.z <= room.maxZ) continue;
                int dist = System.Math.Abs(f.x - cx) + System.Math.Abs(f.z - cz);
                if (dist < bestDist) { bestDist = dist; best = f; }
            }
            return best;
        }

        private void CarveAxisLine(int x1, int z1, int x2, int z2, HashSet<IntVec3> allWalls)
        {
            if (x1 == x2)
            {
                int lo = System.Math.Min(z1, z2), hi = System.Math.Max(z1, z2);
                for (int z = lo; z <= hi; z++) CarveFloorCell(x1, z, allWalls);
            }
            else
            {
                int lo = System.Math.Min(x1, x2), hi = System.Math.Max(x1, x2);
                for (int x = lo; x <= hi; x++) CarveFloorCell(x, z1, allWalls);
            }
        }

        
        
        private void ConnectRoomToExistingNetwork(Rect room)
        {
            int ax = room.minX + room.width / 2;
            int az = room.minZ + room.depth / 2;

            IntVec3 target = FindNearestNetworkCell(room);
            if (!target.IsValid) return;

            CarveAxisLine(ax, az, target.x, az);
            CarveAxisLine(target.x, az, target.x, target.z);
        }

        
        private void ConnectRectToExistingNetwork(Rect rect)
        {
            int cx = rect.minX + rect.width / 2;
            int cz = rect.minZ + rect.depth / 2;

            IntVec3 target = FindNearestNetworkCellForPoint(cx, cz, rect);
            if (!target.IsValid) return;

            CarveAxisLine(cx, cz, target.x, cz);
            CarveAxisLine(target.x, cz, target.x, target.z);
        }

        
        private IntVec3 FindNearestNetworkCellForPoint(int cx, int cz, Rect excludeRect)
        {
            IntVec3 best = IntVec3.Invalid;
            int bestDist = int.MaxValue;
            foreach (var f in _floorCells)
            {
                
                if (f.x >= excludeRect.minX && f.x <= excludeRect.maxX && f.z >= excludeRect.minZ && f.z <= excludeRect.maxZ) continue;
                int dist = System.Math.Abs(f.x - cx) + System.Math.Abs(f.z - cz);
                if (dist < bestDist) { bestDist = dist; best = f; }
            }
            return best;
        }

        private void CarveAxisLine(int x1, int z1, int x2, int z2)
        {
            if (x1 == x2)
            {
                int lo = System.Math.Min(z1, z2), hi = System.Math.Max(z1, z2);
                for (int z = lo; z <= hi; z++) CarveFloorCellSimple(x1, z);
            }
            else
            {
                int lo = System.Math.Min(x1, x2), hi = System.Math.Max(x1, x2);
                for (int x = lo; x <= hi; x++) CarveFloorCellSimple(x, z1);
            }
        }

        
        private void CarveFloorCellSimple(int x, int z)
        {
            var cell = new IntVec3(x, 0, z);
            if (!cell.InBounds(_map)) return;
            _floorCells.Add(cell);
        }

        private void FillAreaWithConnectedSmallRooms(int areaX, int areaZ, int areaW, int areaD, List<Rect> structures)
        {
            int roomsW = Rand.RangeInclusive(1, 3);
            int roomsD = Rand.RangeInclusive(1, 3);
            int cellW = (areaW - (roomsW + 1)) / roomsW;
            int cellD = (areaD - (roomsD + 1)) / roomsD;
            if (cellW < RoomMin || cellD < RoomMin) return;

            for (int ri = 0; ri < roomsW; ri++)
            {
                for (int rj = 0; rj < roomsD; rj++)
                {
                    int rx = areaX + 1 + ri * (cellW + 1);
                    int rz = areaZ + 1 + rj * (cellD + 1);

                    int rw = cellW;
                    int rd = cellD;

                    var rect = new Rect { minX = rx, minZ = rz, width = rw, depth = rd };
                    if (Commit(rect))
                    {
                        structures.Add(rect);
                        
                        ConnectRoomToExistingNetwork(rect);
                    }
                }
            }
        }

        private void FillAreaWithBigRoomAndCorridor(int areaX, int areaZ, int areaW, int areaD, List<Rect> structures)
        {
            
            int rw = Rand.RangeInclusive(RoomMin, System.Math.Min(areaW - 3, RoomMax));
            int rd = Rand.RangeInclusive(RoomMin, System.Math.Min(areaD - 3, RoomMax));

            bool tryFromLeft = areaX > 2;
            bool tryFromRight = areaX + areaW < _mapSizeX - 2;
            bool tryFromBottom = areaZ > 2;
            bool tryFromTop = areaZ + areaD < _mapSizeZ - 2;

            var dirs = new List<(int dx, int dz)>();
            if (tryFromLeft) dirs.Add((-1, 0));
            if (tryFromRight) dirs.Add((1, 0));
            if (tryFromBottom) dirs.Add((0, -1));
            if (tryFromTop) dirs.Add((0, 1));
            if (dirs.Count == 0) return;

            var (ddx, ddz) = dirs[Rand.Range(0, dirs.Count)];

            int roomX, roomZ;
            if (ddx != 0)
            {
                roomZ = areaZ + (areaD - rd) / 2;
                if (ddx > 0)
                    roomX = areaX + areaW - rw - 1;
                else
                    roomX = areaX + 1;
            }
            else
            {
                roomX = areaX + (areaW - rw) / 2;
                if (ddz > 0)
                    roomZ = areaZ + areaD - rd - 1;
                else
                    roomZ = areaZ + 1;
            }

            var room = new Rect { minX = roomX, minZ = roomZ, width = rw, depth = rd };
            if (!Commit(room)) return;
            structures.Add(room);
            ConnectRoomToExistingNetwork(room);

            int corridorLen;
            IntVec2 corridorStart, corridorDir;
            if (ddx != 0)
            {
                corridorLen = ddx > 0
                    ? roomX - areaX - areaW
                    : areaX - roomX - rw;
                if (corridorLen < 1) return;
                int corX = ddx > 0 ? areaX + areaW : roomX + rw;
                int corZ = roomZ + rd / 2;
                corridorStart = new IntVec2(corX, corZ);
                corridorDir = new IntVec2(ddx, 0);
            }
            else
            {
                corridorLen = ddz > 0
                    ? roomZ - areaZ - areaD
                    : areaZ - roomZ - rd;
                if (corridorLen < 1) return;
                int corX = roomX + rw / 2;
                int corZ = ddz > 0 ? areaZ + areaD : roomZ + rd;
                corridorStart = new IntVec2(corX, corZ);
                corridorDir = new IntVec2(0, ddz);
            }

            var seg = SegmentRect(corridorStart, corridorDir, 1, corridorLen);
            if (seg.HasValue)
            {
                Commit(seg.Value);
                ConnectRectToExistingNetwork(seg.Value);
            }
        }

        private void FillAreaWithNormalRooms(int areaX, int areaZ, int areaW, int areaD, List<Rect> structures)
        {
            int attempts = 10;
            for (int i = 0; i < attempts; i++)
            {
                int rw = Rand.RangeInclusive(RoomMin, System.Math.Min(RoomMax, areaW - 2));
                int rd = Rand.RangeInclusive(RoomMin, System.Math.Min(RoomMax, areaD - 2));
                int rx = areaX + Rand.Range(1, areaW - rw - 1);
                int rz = areaZ + Rand.Range(1, areaD - rd - 1);

                var rect = new Rect { minX = rx, minZ = rz, width = rw, depth = rd };
                if (Commit(rect))
                {
                    structures.Add(rect);
                    ConnectRoomToExistingNetwork(rect);
                    break;
                }
            }
        }

        private void CreateMapEdgeExits(HashSet<IntVec3> allWalls)
        {
            var roomGroups = GetFloorGroups();

            foreach (var group in roomGroups)
            {
                var edgeDirs = new HashSet<int>();
                foreach (var c in group)
                {
                    if (c.x <= 1) edgeDirs.Add(1);
                    if (c.x >= _mapSizeX - 2) edgeDirs.Add(2);
                    if (c.z <= 1) edgeDirs.Add(3);
                    if (c.z >= _mapSizeZ - 2) edgeDirs.Add(4);
                }

                if (edgeDirs.Count == 0)
                {
                    TryCreateCorridorToEdgePrespawn(group, allWalls);
                    continue;
                }

                int[] dirs = new List<int>(edgeDirs).ToArray();
                int chosenDir = dirs[Rand.Range(0, dirs.Length)];

                if (Rand.Value < 0.1f)
                    OpenEntireRoomSide(group, chosenDir, allWalls);
                else if (Rand.Value < 0.5f)
                    OpenSingleExit(group, chosenDir, allWalls);
            }
        }

        private IntVec3 EdgeDirToDelta(int edgeDir)
        {
            return edgeDir switch
            {
                1 => new IntVec3(-1, 0, 0),
                2 => new IntVec3(1, 0, 0),
                3 => new IntVec3(0, 0, -1),
                4 => new IntVec3(0, 0, 1),
                _ => IntVec3.Invalid
            };
        }

        private bool IsOnEdgeSide(IntVec3 c, int edgeDir)
        {
            return edgeDir switch
            {
                1 => c.x <= 1,
                2 => c.x >= _mapSizeX - 2,
                3 => c.z <= 1,
                4 => c.z >= _mapSizeZ - 2,
                _ => false
            };
        }

        private void OpenSingleExit(HashSet<IntVec3> group, int edgeDir, HashSet<IntVec3> allWalls)
        {
            var borderCells = new List<IntVec3>();
            IntVec3 delta = EdgeDirToDelta(edgeDir);
            foreach (var c in group)
            {
                var wallCell = c + delta;
                if (wallCell.InBounds(_map) && allWalls.Contains(wallCell))
                    borderCells.Add(wallCell);
            }
            if (borderCells.Count == 0) return;

            var target = borderCells[Rand.Range(0, borderCells.Count)];
            allWalls.Remove(target);
        }

        private void OpenEntireRoomSide(HashSet<IntVec3> group, int edgeDir, HashSet<IntVec3> allWalls)
        {
            IntVec3 delta = EdgeDirToDelta(edgeDir);
            foreach (var c in group)
            {
                var wallCell = c + delta;
                if (wallCell.InBounds(_map) && allWalls.Contains(wallCell))
                    allWalls.Remove(wallCell);
            }
        }

        private void TryCreateCorridorToEdgePrespawn(HashSet<IntVec3> group, HashSet<IntVec3> allWalls)
        {
            if (Rand.Value > 0.15f) return;

            var candidates = new List<(IntVec3 start, int dir)>();
            foreach (var c in group)
            {
                if (c.x >= 4) candidates.Add((c, 1));
                if (c.x <= _mapSizeX - 5) candidates.Add((c, 2));
                if (c.z >= 4) candidates.Add((c, 3));
                if (c.z <= _mapSizeZ - 5) candidates.Add((c, 4));
            }
            if (candidates.Count == 0) return;

            var (start, dir) = candidates[Rand.Range(0, candidates.Count)];

            int corridorWidth = Rand.RangeInclusive(1, 3);

            int turns = Rand.RangeInclusive(0, 2);
            var cursor = new IntVec2(start.x, start.z);
            var curDir = EdgeDirToDelta(dir);
            var curDir2 = new IntVec2(curDir.x, curDir.z);

            int remaining = (_mapSizeX + _mapSizeZ) / 2;

            for (int seg = 0; seg <= turns && remaining > 0; seg++)
            {
                int maxSeg = seg < turns ? System.Math.Min(remaining, Rand.RangeInclusive(2, 8)) : remaining;
                if (maxSeg < 1) break;

                var segRect = SegmentRect(cursor, curDir2, corridorWidth, maxSeg);
                if (segRect == null) break;

                if (Collides(segRect.Value))
                    break;

                if (!Commit(segRect.Value))
                    break;

                if (seg < turns)
                {
                    if (curDir2.z != 0)
                        cursor = new IntVec2(cursor.x, curDir2.z > 0 ? segRect.Value.maxZ : segRect.Value.minZ);
                    else
                        cursor = new IntVec2(curDir2.x > 0 ? segRect.Value.maxX : segRect.Value.minX, cursor.z);

                    curDir2 = Rand.Bool ? new IntVec2(-curDir2.z, curDir2.x) : new IntVec2(curDir2.z, -curDir2.x);
                }

                remaining -= maxSeg;
            }
        }

        
        
        
        
        
        
        
        private void EnsureExitsOnMultipleSides(HashSet<IntVec3> allWalls)
        {
            var groups = GetFloorGroups();
            if (groups.Count == 0) return;
            
            groups.Sort((a, b) => b.Count.CompareTo(a.Count));
            var main = groups[0];

            
            
            
            
            int safety = 8;
            while (safety-- > 0)
            {
                var openSides = SidesWithEdgeExitFromGroup(main, allWalls);
                if (openSides.Count >= 2)
                {
                    RoomsLog.Message($"[Rooms] Exits on {openSides.Count} sides: {string.Join(",", openSides)} — OK");
                    return;
                }

                var allSides = new List<int> { 1, 2, 3, 4 };
                var want = allSides.FindAll(s => !openSides.Contains(s));
                if (want.Count == 0) return;

                int cx = 0, cz = 0;
                foreach (var c in main) { cx += c.x; cz += c.z; }
                cx /= main.Count; cz /= main.Count;

                int bestSide = -1, bestDist = -1;
                foreach (var s in want)
                {
                    int dist = s switch
                    {
                        1 => cx,                       
                        2 => _mapSizeX - 1 - cx,       
                        3 => cz,                       
                        4 => _mapSizeZ - 1 - cz,       
                        _ => 0
                    };
                    if (dist > bestDist) { bestDist = dist; bestSide = s; }
                }
                if (bestSide < 0) return;

                RoomsLog.Message($"[Rooms] Carving exit toward side {bestSide} (open sides were: {(openSides.Count == 0 ? "none" : string.Join(",", openSides))})");
                CarveExitToSpecificSide(main, bestSide, allWalls);
            }
        }

        
        
        
        
        private HashSet<int> SidesWithEdgeExitFromGroup(HashSet<IntVec3> group, HashSet<IntVec3> allWalls)
        {
            var openSides = new HashSet<int>();
            if (group == null || group.Count == 0) return openSides;

            IntVec3 start = IntVec3.Invalid;
            foreach (var c in group) { start = c; break; }
            if (!start.IsValid) return openSides;

            var visited = new HashSet<IntVec3>();
            var queue = new Queue<IntVec3>();
            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                if (cur.x == 0) openSides.Add(1);
                if (cur.x == _mapSizeX - 1) openSides.Add(2);
                if (cur.z == 0) openSides.Add(3);
                if (cur.z == _mapSizeZ - 1) openSides.Add(4);

                foreach (var adj in GenAdj.AdjacentCells)
                {
                    var next = cur + adj;
                    if (!next.InBounds(_map)) continue;
                    if (visited.Contains(next)) continue;
                    bool walkable = _floorCells.Contains(next) || !allWalls.Contains(next);
                    if (walkable) { visited.Add(next); queue.Enqueue(next); }
                }
            }
            return openSides;
        }

        
        
        private void CarveExitToSpecificSide(HashSet<IntVec3> group, int side, HashSet<IntVec3> allWalls)
        {
            
            IntVec3 anchor = IntVec3.Invalid;
            int bestDist = int.MaxValue;
            foreach (var c in group)
            {
                int d = side switch
                {
                    1 => c.x,
                    2 => _mapSizeX - 1 - c.x,
                    3 => c.z,
                    4 => _mapSizeZ - 1 - c.z,
                    _ => int.MaxValue
                };
                if (d < bestDist) { bestDist = d; anchor = c; }
            }
            if (!anchor.IsValid || bestDist == int.MaxValue) return;

            var dirDelta = EdgeDirToDelta(side);
            var dirVec = new IntVec2(dirDelta.x, dirDelta.z);
            var carveStart = new IntVec2(anchor.x + dirDelta.x, anchor.z + dirDelta.z);
            
            CarveSingleCorridor(carveStart, dirVec, width: 1, length: bestDist, allWalls);
        }

        private void EnsureFinalExitExists(HashSet<IntVec3> allWalls)
        {
            if (HasPathToEdge(allWalls))
            {
                RoomsLog.Message($"[Rooms] Final check: path to edge OK");
                return;
            }

            RoomsLog.Warning($"[Rooms] Final check: NO path to edge! Forcing corridor.");
            var groups = GetFloorGroups();
            groups.Sort((a, b) => b.Count.CompareTo(a.Count));

            foreach (var group in groups)
            {
                CarveExitCorridorFromGroup(group, allWalls);
                if (HasPathToEdge(allWalls))
                {
                    RoomsLog.Message($"[Rooms] Final check: corridor carved, path OK. Walls: {allWalls.Count}, floors: {_floorCells.Count}");
                    return;
                }
            }
        }

        private void EnsureConnectivityToEdge(HashSet<IntVec3> allWalls)
        {
            if (HasPathToEdge(allWalls)) return;

            int count = 1;
            if (Rand.Value < 0.5f) count = 2;
            if (Rand.Value < 0.25f) count = 3;

            var groups = GetFloorGroups();
            if (groups.Count == 0) return;

            var usedGroups = new List<HashSet<IntVec3>>();
            for (int i = 0; i < count && i < groups.Count; i++)
            {
                var available = groups.FindAll(g => !usedGroups.Contains(g));
                if (available.Count == 0) break;
                var group = available[Rand.Range(0, available.Count)];
                usedGroups.Add(group);
                CarveExitCorridorFromGroup(group, allWalls);
            }
        }

        private bool HasPathToEdge(HashSet<IntVec3> allWalls)
        {
            IntVec3 start = IntVec3.Invalid;
            foreach (var c in _floorCells)
            {
                if (c.InBounds(_map)) { start = c; break; }
            }
            if (!start.IsValid) return false;

            var visited = new HashSet<IntVec3>();
            var queue = new Queue<IntVec3>();
            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();

                if (cur.x == 0 || cur.x == _mapSizeX - 1 || cur.z == 0 || cur.z == _mapSizeZ - 1)
                    return true;

                foreach (var adj in GenAdj.AdjacentCells)
                {
                    var next = cur + adj;
                    if (!next.InBounds(_map)) continue;
                    if (visited.Contains(next)) continue;

                    bool walkable = _floorCells.Contains(next) || !allWalls.Contains(next);

                    if (walkable)
                    {
                        visited.Add(next);
                        queue.Enqueue(next);
                    }
                }
            }
            return false;
        }

        private List<HashSet<IntVec3>> GetFloorGroups()
        {
            var groups = new List<HashSet<IntVec3>>();
            var visited = new HashSet<IntVec3>();
            foreach (var cell in _floorCells)
            {
                if (visited.Contains(cell)) continue;
                var group = new HashSet<IntVec3>();
                var queue = new Queue<IntVec3>();
                queue.Enqueue(cell);
                visited.Add(cell);
                while (queue.Count > 0)
                {
                    var cur = queue.Dequeue();
                    group.Add(cur);
                    foreach (var adj in GenAdj.AdjacentCells)
                    {
                        var next = cur + adj;
                        if (next.InBounds(_map) && _floorCells.Contains(next) && !visited.Contains(next))
                        {
                            visited.Add(next);
                            queue.Enqueue(next);
                        }
                    }
                }
                if (group.Count > 4)
                    groups.Add(group);
            }
            return groups;
        }

        private void CarveExitCorridorFromGroup(HashSet<IntVec3> group, HashSet<IntVec3> allWalls)
        {
            int corridorWidth = Rand.RangeInclusive(1, 2);

            int bestDir = 0;
            int bestDist = int.MaxValue;
            int startX = 0, startZ = 0;
            foreach (var c in group)
            {
                int dWest = c.x;
                int dEast = _mapSizeX - 1 - c.x;
                int dSouth = c.z;
                int dNorth = _mapSizeZ - 1 - c.z;

                if (dWest >= 1 && dWest < bestDist) { bestDist = dWest; bestDir = 1; startX = c.x; startZ = c.z; }
                if (dEast >= 1 && dEast < bestDist) { bestDist = dEast; bestDir = 2; startX = c.x; startZ = c.z; }
                if (dSouth >= 1 && dSouth < bestDist) { bestDist = dSouth; bestDir = 3; startX = c.x; startZ = c.z; }
                if (dNorth >= 1 && dNorth < bestDist) { bestDist = dNorth; bestDir = 4; startX = c.x; startZ = c.z; }
            }
            if (bestDir == 0) return;

            var dirDelta = EdgeDirToDelta(bestDir);
            var dirVec = new IntVec2(dirDelta.x, dirDelta.z);

            var carveStart = new IntVec2(startX + dirDelta.x, startZ + dirDelta.z);
            CarveSingleCorridor(carveStart, dirVec, corridorWidth, bestDist, allWalls);
        }

        private void CarveSingleCorridor(IntVec2 start, IntVec2 dir, int width, int length, HashSet<IntVec3> allWalls)
        {
            int halfW = width / 2;
            int removed = 0, floorBefore = _floorCells.Count;
            for (int i = 0; i < length; i++)
            {
                int cx = start.x + dir.x * i;
                int cz = start.z + dir.z * i;
                for (int wi = 0; wi < width; wi++)
                {
                    int wx = dir.z != 0 ? cx + wi - halfW : cx;
                    int wz = dir.z != 0 ? cz : cz + wi - halfW;
                    var cell = new IntVec3(wx, 0, wz);
                    if (!cell.InBounds(_map)) continue;
                    if (allWalls.Remove(cell))
                        removed++;
                    if (!_floorCells.Contains(cell))
                        _floorCells.Add(cell);
                }
            }
            RoomsLog.Message($"[Rooms] CarveSingleCorridor: start=({start.x},{start.z}) dir=({dir.x},{dir.z}) w={width} len={length} removed={removed} floorBefore={floorBefore} floorAfter={_floorCells.Count}");
        }

        
        
        
        protected struct Rect
        {
            public int minX, minZ, width, depth;
            public int maxX => minX + width - 1;
            public int maxZ => minZ + depth - 1;
        }

        
        
        protected List<Rect> _generatedStructures = new List<Rect>();

        private bool InBounds(IntVec2 c) => c.x >= 1 && c.z >= 1 && c.x < _mapSizeX - 1 && c.z < _mapSizeZ - 1;

        private bool Collides(Rect r)
        {
            for (int x = r.minX; x <= r.maxX; x++)
                for (int z = r.minZ; z <= r.maxZ; z++)
                    if (_floorCells.Contains(new IntVec3(x, 0, z)))
                        return true;
            return false;
        }

        private bool Commit(Rect r)
        {
            if (!InBounds(new IntVec2(r.minX, r.minZ)) || !InBounds(new IntVec2(r.maxX, r.maxZ)))
                return false;
            if (Collides(r)) return false;

            for (int x = r.minX; x <= r.maxX; x++)
                for (int z = r.minZ; z <= r.maxZ; z++)
                    _floorCells.Add(new IntVec3(x, 0, z));

            OutlineWalls(r);
            return true;
        }

        private void OutlineWalls(Rect r)
        {
            for (int x = r.minX - 1; x <= r.maxX + 1; x++)
            {
                AddWall(new IntVec3(x, 0, r.minZ - 1));
                AddWall(new IntVec3(x, 0, r.maxZ + 1));
            }
            for (int z = r.minZ - 1; z <= r.maxZ + 1; z++)
            {
                AddWall(new IntVec3(r.minX - 1, 0, z));
                AddWall(new IntVec3(r.maxX + 1, 0, z));
            }
        }

        private void AddWall(IntVec3 c)
        {
            if (_floorCells.Contains(c)) return;
            _wallCells.Add(c);
        }

        private Rect MakeRoomRectNear(IntVec2 center)
        {
            int w = Rand.RangeInclusive(RoomMin, RoomMax);
            int d = Rand.RangeInclusive(RoomMin, RoomMax);
            return new Rect { minX = center.x - w / 2, minZ = center.z - d / 2, width = w, depth = d };
        }

        
        
        private IntVec2 RandomSeedPoint()
        {
            int margin = System.Math.Max(12, _mapSizeX / 6);
            int x = Rand.RangeInclusive(margin, _mapSizeX - 1 - margin);
            int z = Rand.RangeInclusive(margin, _mapSizeZ - 1 - margin);
            return new IntVec2(x, z);
        }

        
        
        
        
        
        
        
        private Rect MakeBigHallRect()
        {
            const int BigHallMaxLong = 50;
            const int BigHallMaxShort = 25;
            const int BigHallMin = 12;

            int sideA = Rand.RangeInclusive(BigHallMin, BigHallMaxLong);
            int sideB = Rand.RangeInclusive(BigHallMin, BigHallMaxShort);
            
            
            int w, d;
            if (sideA >= sideB) { w = sideA; d = sideB; }
            else { w = sideB; d = sideA; }

            
            int mapW = _mapSizeX - 2; 
            int mapD = _mapSizeZ - 2;
            if (w > mapW) w = mapW;
            if (d > mapD) d = mapD;
            if (w < BigHallMin || d < BigHallMin) return new Rect { minX = 0, minZ = 0, width = 0, depth = 0 }; 

            int halfW = w / 2, halfD = d / 2;
            int margin = System.Math.Max(halfW, halfD) + 2;
            
            
            int minCx = margin;
            int maxCx = _mapSizeX - 1 - margin;
            int minCz = margin;
            int maxCz = _mapSizeZ - 1 - margin;
            
            if (minCx > maxCx || minCz > maxCz) return new Rect { minX = 0, minZ = 0, width = 0, depth = 0 }; 

            int cx = Rand.RangeInclusive(minCx, maxCx);
            int cz = Rand.RangeInclusive(minCz, maxCz);
            
            
            int minX = cx - halfW;
            int minZ = cz - halfD;
            int maxX = cx + halfW + (w % 2); 
            int maxZ = cz + halfD + (d % 2);
            
            if (minX < 1 || minZ < 1 || maxX >= _mapSizeX - 1 || maxZ >= _mapSizeZ - 1)
            {
                
                if (minX < 1) { cx += 1 - minX; }
                if (minZ < 1) { cz += 1 - minZ; }
                if (maxX >= _mapSizeX - 1) { cx -= maxX - (_mapSizeX - 2); }
                if (maxZ >= _mapSizeZ - 1) { cz -= maxZ - (_mapSizeZ - 2); }
                minX = cx - halfW;
                minZ = cz - halfD;
            }
            
            return new Rect { minX = minX, minZ = minZ, width = w, depth = d };
        }

        private bool TryPickEdge(Rect r, out IntVec2 edge, out IntVec2 dir)
        {
            dir = new IntVec2(0, 0);
            edge = new IntVec2(0, 0);
            switch (Rand.RangeInclusive(0, 3))
            {
                case 0: dir = new IntVec2(0, -1); edge = new IntVec2(Rand.RangeInclusive(r.minX, r.maxX), r.minZ); break;
                case 1: dir = new IntVec2(0, 1);  edge = new IntVec2(Rand.RangeInclusive(r.minX, r.maxX), r.maxZ); break;
                case 2: dir = new IntVec2(-1, 0); edge = new IntVec2(r.minX, Rand.RangeInclusive(r.minZ, r.maxZ)); break;
                case 3: dir = new IntVec2(1, 0);  edge = new IntVec2(r.maxX, Rand.RangeInclusive(r.minZ, r.maxZ)); break;
            }
            return true;
        }

        private Rect? TryGrowRoom(IntVec2 edge, IntVec2 dir)
        {
            int w = Rand.RangeInclusive(RoomMin, RoomMax);
            int d = Rand.RangeInclusive(RoomMin, RoomMax);
            Rect r;
            if (dir.z != 0)
            {
                int nearZ = edge.z + dir.z;
                int minZ = dir.z > 0 ? nearZ : nearZ - d + 1;
                int minX = Rand.RangeInclusive(edge.x - w + 1, edge.x);
                r = new Rect { minX = minX, minZ = minZ, width = w, depth = d };
            }
            else
            {
                int nearX = edge.x + dir.x;
                int minX = dir.x > 0 ? nearX : nearX - w + 1;
                int minZ = Rand.RangeInclusive(edge.z - d + 1, edge.z);
                r = new Rect { minX = minX, minZ = minZ, width = w, depth = d };
            }
            if (!Commit(r)) return null;
            return r;
        }

        private Rect? TryGrowTrunkCorridor(IntVec2 edge, IntVec2 dir, List<Rect> structures)
        {
            int width = Rand.RangeInclusive(CorridorWidthMin, CorridorWidthMax);
            int length = Rand.RangeInclusive(CorridorSegmentMin, CorridorSegmentMax);
            var corridor = SegmentRect(edge, dir, width, length);
            if (corridor == null || !Commit(corridor.Value)) return null;
            Rect c = corridor.Value;

            TrySideRooms(c, dir, leftFlank: true, structures);
            TrySideRooms(c, dir, leftFlank: false, structures);
            var endRoom = TryRoomAtEnd(c, dir);
            if (endRoom != null) structures.Add(endRoom.Value);
            return c;
        }

        private Rect? TryGrowWindingCorridor(IntVec2 edge, IntVec2 dir)
        {
            IntVec2 cursor = edge;
            IntVec2 cur = dir;
            bool committedAny = false;
            int turns = Rand.RangeInclusive(2, 3);
            for (int t = 0; t <= turns; t++)
            {
                int seg = Rand.RangeInclusive(CorridorSegmentMin, CorridorSegmentMax);
                int w = Rand.RangeInclusive(CorridorWidthMin, CorridorWidthMax);
                var segRect = SegmentRect(cursor, cur, w, seg);
                if (segRect == null || Collides(segRect.Value))
                {
                    if (!committedAny) return null;
                    break;
                }
                if (!Commit(segRect.Value)) break;
                committedAny = true;
                if (cur.z != 0)
                    cursor = new IntVec2(cursor.x, cur.z > 0 ? segRect.Value.maxZ : segRect.Value.minZ);
                else
                    cursor = new IntVec2(cur.x > 0 ? segRect.Value.maxX : segRect.Value.minX, cursor.z);
                if (!InBounds(cursor)) break;
                cur = Rand.Bool ? new IntVec2(-cur.z, cur.x) : new IntVec2(cur.z, -cur.x);
            }
            if (!committedAny) return null;

            return TryRoomAtCell(cursor);
        }

        private Rect? SegmentRect(IntVec2 start, IntVec2 dir, int width, int length)
        {
            int w, d, minX, minZ;
            if (dir.z != 0)
            {
                w = width; d = length;
                minX = start.x - width / 2;
                minZ = dir.z > 0 ? start.z + dir.z : start.z + dir.z - length + 1;
            }
            else
            {
                w = length; d = width;
                minX = dir.x > 0 ? start.x + dir.x : start.x + dir.x - length + 1;
                minZ = start.z - width / 2;
            }
            var r = new Rect { minX = minX, minZ = minZ, width = w, depth = d };
            if (!InBounds(new IntVec2(r.minX, r.minZ)) || !InBounds(new IntVec2(r.maxX, r.maxZ)))
                return null;
            return r;
        }

        private void TrySideRooms(Rect c, IntVec2 dir, bool leftFlank, List<Rect> structures)
        {
            IntVec2 flankDir = leftFlank ? new IntVec2(-dir.z, dir.x) : new IntVec2(dir.z, -dir.x);
            int steps = 3;
            for (int i = 0; i < steps; i++)
            {
                IntVec2 pt;
                if (dir.z != 0)
                    pt = new IntVec2(c.minX + Rand.RangeInclusive(0, c.width - 1),
                                     c.minZ + (c.depth - 1) * i / System.Math.Max(1, steps - 1));
                else
                    pt = new IntVec2(c.minX + (c.width - 1) * i / System.Math.Max(1, steps - 1),
                                     c.minZ + Rand.RangeInclusive(0, c.depth - 1));
                var room = TryGrowRoom(pt, flankDir);
                if (room != null) structures.Add(room.Value);
            }
        }

        private Rect? TryRoomAtEnd(Rect corridor, IntVec2 dir)
        {
            IntVec2 end;
            if (dir.z != 0)
                end = new IntVec2(corridor.minX + corridor.width / 2, dir.z > 0 ? corridor.maxZ : corridor.minZ);
            else
                end = new IntVec2(dir.x > 0 ? corridor.maxX : corridor.minX, corridor.minZ + corridor.depth / 2);
            return TryRoomAtCell(end);
        }

        private Rect? TryRoomAtCell(IntVec2 at)
        {
            var r = MakeRoomRectNear(at);
            if (Commit(r)) return r;
            return null;
        }

        private void EnsureAllFloorGroupsConnected(HashSet<IntVec3> allWalls)
        {
            var groups = GetFloorGroups();
            if (groups.Count <= 1) return;

            groups.Sort((a, b) => b.Count.CompareTo(a.Count));
            var mainGroup = groups[0];
            int linked = 0;

            for (int i = 1; i < groups.Count; i++)
            {
                var smallGroup = groups[i];

                IntVec3 bestFrom = IntVec3.Invalid;
                IntVec3 bestTo = IntVec3.Invalid;
                int bestDist = int.MaxValue;

                foreach (var from in smallGroup)
                {
                    foreach (var to in mainGroup)
                    {
                        int dist = System.Math.Abs(from.x - to.x) + System.Math.Abs(from.z - to.z);
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestFrom = from;
                            bestTo = to;
                        }
                    }
                }

                if (!bestFrom.IsValid || !bestTo.IsValid) continue;

                CarveAxisLine(bestFrom.x, bestFrom.z, bestTo.x, bestFrom.z, allWalls);
                CarveAxisLine(bestTo.x, bestFrom.z, bestTo.x, bestTo.z, allWalls);
                linked++;
            }

            if (linked > 0)
                RoomsLog.Message($"[Rooms] Connected {linked} isolated group(s) to main network");
        }
    }
}
