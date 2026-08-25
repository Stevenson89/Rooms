using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace arsiy.Rooms.Things
{
    
    
    
    
    
    
    
    public class WorkGiver_YRCarryToPit : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override Danger MaxPathDanger(Pawn pawn)
        {
            return Danger.Deadly;
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            if (pawn == null || pawn.Map == null) { RoomsLog.Message("[YRPIT] ShouldSkip true (no pawn/map)"); return true; }
            foreach (var t in pawn.Map.listerThings.ThingsOfDef(ThingDef.Named("YellowRooms_Pit")))
            {
                if (YellowRoomsPitTransfer.CompFor(t) is IYellowRoomsPitCargo c && c.HasItemsPending)
                    return false;
            }
            RoomsLog.Message("[YRPIT] ShouldSkip true (no pits with pending items on map)");
            return true;
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            foreach (var t in pawn.Map.listerThings.ThingsOfDef(ThingDef.Named("YellowRooms_Pit")))
                yield return t;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t == null) return false;
            var c = YellowRoomsPitTransfer.CompFor(t);
            if (c == null) { RoomsLog.Message("[YRPIT] HasJobOnThing false: no CompFor on " + t); return false; }
            if (!c.HasItemsPending) { RoomsLog.Message("[YRPIT] HasJobOnThing false: no items pending on " + t); return false; }
            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)) return false;
            if (!pawn.CanReach(t, PathEndMode.Touch, pawn.NormalMaxDanger())) { RoomsLog.Message("[YRPIT] HasJobOnThing false: cannot reach " + t + " pawn=" + pawn); return false; }
            if (YellowRoomsPitTransfer.FindThingToLoad(pawn, c).Thing == null) { RoomsLog.Message("[YRPIT] HasJobOnThing false: FindThingToLoad null for pawn " + pawn); return false; }
            if (!pawn.CanReserve(t, 1, -1, null, forced)) return false;
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var c = YellowRoomsPitTransfer.CompFor(t);
            return c != null ? YellowRoomsPitTransfer.MakeCarryJob(pawn, c, forced) : null;
        }
    }
}