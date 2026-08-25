using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace arsiy.Rooms.Things
{
    
    
    
    
    
    
    
    
    public class WorkGiver_YRRepairPortal : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);

        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override Danger MaxPathDanger(Pawn pawn)
        {
            return Danger.Deadly;
        }

        private static List<ThingDef> portalDefsCache;

        private static List<ThingDef> PortalDefs
        {
            get
            {
                if (portalDefsCache == null)
                {
                    portalDefsCache = new List<ThingDef>(4);
                    foreach (var name in new[]
                             {
                                 "YellowRooms_DoorToRooms", "YellowRooms_PortalToRooms",
                                 "YellowRooms_ReturnDoor", "YellowRooms_PortalFromRooms"
                             })
                    {
                        var def = ThingDef.Named(name);
                        if (def != null) portalDefsCache.Add(def);
                    }
                }
                return portalDefsCache;
            }
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            foreach (var def in PortalDefs)
            {
                foreach (var t in pawn.Map.listerThings.ThingsOfDef(def))
                    yield return t;
            }
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            var des = arsiy.Rooms.DesignationDefOf_Rooms.YellowRooms_RepairPortal;
            if (des == null) return true;
            foreach (var def in PortalDefs)
            {
                foreach (var t in pawn.Map.listerThings.ThingsOfDef(def))
                {
                    if (pawn.Map.designationManager.DesignationOn(t, des) != null)
                        return false;
                }
            }
            return true;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var des = arsiy.Rooms.DesignationDefOf_Rooms.YellowRooms_RepairPortal;
            if (des == null) return false;
            if (t.Faction != pawn.Faction) return false;
            if (pawn.Map.designationManager.DesignationOn(t, des) == null) return false;
            if (!PortalIsBroken(t)) return false;
            if (!ResourcesAvailable(pawn.Map))
            {
                JobFailReason.Is("YellowRooms_RepairNoResources".Translate());
                return false;
            }
            if (!pawn.CanReserve(t, 1, -1, null, forced)) return false;
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(JobDefOf_Rooms.YellowRooms_RepairPortal, t);
        }

        
        public static bool PortalIsBroken(Thing t)
        {
            var dc = t?.TryGetComp<Comp_DoorToRooms>();
            if (dc != null && dc.IsBroken) return true;
            var rc = t?.TryGetComp<Comp_YellowRoomsReturnDoor>();
            return rc != null && rc.IsBroken;
        }

        private static bool ResourcesAvailable(Map map)
        {
            if (map == null) return false;
            if (CountOnMap(map, ThingDefOf.Steel) < JobDriver_YRRepairPortal.SteelNeeded)
                return false;
            return CountOnMap(map, ThingDefOf.ComponentSpacer) >= JobDriver_YRRepairPortal.SpacerNeeded;
        }

        
        
        
        
        
        
        public static int CountDeliveredNear(Thing portal, ThingDef def)
        {
            var map = portal?.Map;
            if (map == null || def == null) return 0;
            int total = 0;
            foreach (var cell in GenRadial.RadialCellsAround(portal.Position, 3f, true))
            {
                if (!cell.InBounds(map)) continue;
                var list = map.thingGrid.ThingsListAt(cell);
                for (int i = 0; i < list.Count; i++)
                {
                    var th = list[i];
                    if (th != null && th.Spawned && !th.Destroyed && th.def == def)
                        total += th.stackCount;
                }
            }
            return total;
        }

        private static int CountOnMap(Map map, ThingDef def)
        {
            int total = 0;
            foreach (var th in map.listerThings.ThingsOfDef(def))
            {
                if (th != null && th.Spawned && !th.Destroyed)
                    total += th.stackCount;
            }
            return total;
        }
    }
}
