using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace arsiy.Rooms.GenSteps
{
    public class GenStep_YellowRoomsForeignSiteFix : GenStep
    {
        public override int SeedPart => 345678921;

        private const int ZoneSide = 129;

        private const int InterestRectSide = 80;

        public override void Generate(Map map, GenStepParams parms)
        {
            try
            {
                if (map.Parent is not Site site)
                    return;

                bool hasForeignPart = false;
                foreach (var part in site.parts)
                {
                    if (part.def == null || part.def.defName == null || !part.def.defName.StartsWith("YellowRooms"))
                    {
                        hasForeignPart = true;
                        break;
                    }
                }
                if (!hasForeignPart) return;

                var lampDefs = new HashSet<ThingDef>();
                foreach (var td in DefDatabase<ThingDef>.AllDefsListForReading)
                    if (td.defName.StartsWith("YellowRooms_CeilingLamp"))
                        lampDefs.Add(td);

                var zoneRect = CellRect.CenteredOn(map.Center, ZoneSide, ZoneSide).ClipInsideMap(map);

                int removed = 0;
                var allThings = map.listerThings.AllThings;
                for (int i = allThings.Count - 1; i >= 0; i--)
                {
                    var t = allThings[i];
                    if (t.def.category != ThingCategory.Building) continue;
                    if (!zoneRect.Contains(t.Position)) continue;
                    if (lampDefs.Contains(t.def)) continue;
                    t.DeSpawn(DestroyMode.Vanish);
                    removed++;
                }

                foreach (var cell in map.AllCells)
                {
                    if (!zoneRect.Contains(cell))
                        MapGenerator.Elevation[cell] = 1f;
                }

                MapGenerator.PlayerStartSpot = IntVec3.Invalid;

                var interestRect = CellRect.CenteredOn(map.Center, InterestRectSide, InterestRectSide).ClipInsideMap(map);
                MapGenerator.SetVar("RectOfInterest", interestRect);

                RoomsLog.Message($"[Rooms] ForeignSiteFix: site={site.def?.defName} mapSize={map.Size}, zone={zoneRect}, cleared={removed} buildings, elevation locked outside zone");
            }
            catch (Exception e)
            {
                RoomsLog.Error($"[Rooms] ForeignSiteFix threw: {e}");
            }
        }
    }
}
