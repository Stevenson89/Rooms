using System.Collections.Generic;
using RimWorld;
using Verse;
using arsiy.Rooms.Things;

namespace arsiy.Rooms.Incidents
{
    
    
    
    
    
    
    
    public class IncidentWorker_StrangePassage : IncidentWorker
    {
        private const int LifetimeTicks = 60000; 

        private static readonly IntVec3[] Cardinals =
        {
            new IntVec3(1, 0, 0), new IntVec3(-1, 0, 0),
            new IntVec3(0, 0, 1), new IntVec3(0, 0, -1)
        };

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            var map = parms.target as Map;
            
            if (map != null && YellowRoomsUtility.IsYellowRoomsMap(map))
                return false;
            return map != null && FindSpawnCell(map).cell.IsValid;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            var map = parms.target as Map;
            if (map == null) return false;

            var doorDef = ThingDef.Named("YellowRooms_DoorToRooms");
            if (doorDef == null) return false;

            var (cell, rot) = FindSpawnCell(map);
            if (!cell.IsValid) return false;

            var door = GenSpawn.Spawn(doorDef, cell, map, rot, WipeMode.Vanish);

            
            var comp = map.GetComponent<StrangePassageTimerComp>();
            if (comp == null)
            {
                comp = new StrangePassageTimerComp(map);
                map.components.Add(comp);
            }
            comp.ScheduleExpiration(door, LifetimeTicks);

            var letter = LetterMaker.MakeLetter("StrangePassageLabel".Translate(),
                "StrangePassageDesc".Translate(), LetterDefOf.NeutralEvent, door);
            Find.LetterStack.ReceiveLetter(letter);
            return true;
        }

        
        
        
        
        
        
        private static (IntVec3 cell, Rot4 rot) FindSpawnCell(Map map)
        {
            
            var candidates = new List<(IntVec3 cell, Rot4 rot)>();
            int sx = map.Size.x, sz = map.Size.z;
            for (int x = 1; x < sx - 1; x++)
                for (int z = 1; z < sz - 1; z++)
                {
                    var c = new IntVec3(x, 0, z);
                    if (!c.Standable(map)) continue;
                    if (c.GetEdifice(map) != null) continue;
                    
                    foreach (var d in Cardinals)
                    {
                        var n = c + d;
                        if (!n.InBounds(map)) continue;
                        var ed = n.GetEdifice(map);
                        if (ed != null && ed.def.passability == Traversability.Impassable)
                        {
                            candidates.Add((c, RotFromDelta(d)));
                            break;
                        }
                    }
                }

            if (candidates.Count > 0)
                return candidates[Rand.Range(0, candidates.Count)];

            
            return (CellFinder.RandomClosewalkCellNear(map.Center, map, 40), Rot4.North);
        }

        private static Rot4 RotFromDelta(IntVec3 d)
        {
            if (d == IntVec3.North) return Rot4.North;
            if (d == IntVec3.South) return Rot4.South;
            if (d == IntVec3.East) return Rot4.East;
            return Rot4.West;
        }
    }
}
