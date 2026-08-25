using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace arsiy.Rooms.Things
{
    
    
    
    
    
    
    
    
    public class WorkGiver_YRCarryToPortal : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override Danger MaxPathDanger(Pawn pawn)
        {
            return Danger.Deadly;
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            if (pawn == null || pawn.Map == null) return true;
            foreach (var def in PortalDefs)
            {
                foreach (var t in pawn.Map.listerThings.ThingsOfDef(def))
                {
                    if (YellowRoomsPortalTransfer.CompFor(t) is IYellowRoomsPortalCargo c && c.HasItemsPending)
                        return false;
                }
            }
            return true;
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            foreach (var def in PortalDefs)
            {
                foreach (var t in pawn.Map.listerThings.ThingsOfDef(def))
                    yield return t;
            }
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t == null || t.Faction != pawn.Faction) return false;
            var c = YellowRoomsPortalTransfer.CompFor(t);
            if (c == null || !c.HasItemsPending) return false;
            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)) return false;
            if (!pawn.CanReach(t, PathEndMode.Touch, pawn.NormalMaxDanger())) return false;
            if (YellowRoomsPortalTransfer.FindThingToLoad(pawn, c).Thing == null) return false;
            return pawn.CanReserve(t, 1, -1, null, forced);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var c = YellowRoomsPortalTransfer.CompFor(t);
            return c != null ? YellowRoomsPortalTransfer.MakeCarryJob(pawn, c, forced) : null;
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
    }
}
