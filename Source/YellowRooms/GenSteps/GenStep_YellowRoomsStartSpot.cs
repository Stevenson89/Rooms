using System.Collections.Generic;
using RimWorld;
using Verse;

namespace arsiy.Rooms.GenSteps
{
    
    
    
    
    
    
    
    
    
    
    public class GenStep_YellowRoomsStartSpot : GenStep
    {
        public override int SeedPart => 1187186632;

        public override void Generate(Map map, GenStepParams parms)
        {
            if (map.wasSpawnedViaGravShipLanding) return;

            
            DeepProfiler.Start("RebuildAllRegions");
            map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
            DeepProfiler.End();

            if (MapGenerator.PlayerStartSpotValid) return;

            var largestOpenArea = FindLargestContiguousOpenArea(map);
            var usedRects = MapGenerator.GetOrGenerateVar<List<CellRect>>("UsedRects");

            
            
            var center = map.Center;
            int maxRadius = (map.Size.x + map.Size.z) / 2;
            for (int r = 0; r <= maxRadius; r++)
            {
                var candidates = new List<IntVec3>();
                foreach (var c in GenRadial.RadialCellsAround(center, r, useCenter: true))
                {
                    if (!c.InBounds(map)) continue;
                    if (!c.Standable(map)) continue;
                    if (!largestOpenArea.Contains(c)) continue;
                    if (RectContains(usedRects, c)) continue;
                    candidates.Add(c);
                }
                if (candidates.Count == 0) continue;
                MapGenerator.PlayerStartSpot = candidates[Rand.Range(0, candidates.Count)];
                return;
            }

            
            if (CellFinder.TryFindRandomCell(map, c => c.Standable(map), out var fallback))
                MapGenerator.PlayerStartSpot = fallback;
        }

        private static bool RectContains(List<CellRect> rects, IntVec3 c)
        {
            foreach (var rect in rects)
            {
                if (rect.Contains(c)) return true;
            }
            return false;
        }

        
        
        private static HashSet<IntVec3> FindLargestContiguousOpenArea(Map map)
        {
            var checkedCells = new HashSet<IntVec3>();
            var biggest = new HashSet<IntVec3>();
            foreach (var cell in map.AllCells)
            {
                if (checkedCells.Contains(cell)) continue;
                if (cell.GetEdifice(map) != null) continue;

                var current = new HashSet<IntVec3>();
                map.floodFiller.FloodFill(cell,
                    c => c.GetEdifice(map) == null,
                    c => { current.Add(c); checkedCells.Add(c); });
                if (current.Count > biggest.Count)
                {
                    biggest.Clear();
                    biggest.AddRange(current);
                }
            }
            return biggest;
        }
    }
}
