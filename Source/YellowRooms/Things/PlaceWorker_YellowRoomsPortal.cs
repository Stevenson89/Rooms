using RimWorld;
using Verse;

namespace arsiy.Rooms.Things
{
    
    
    
    
    
    
    
    public class PlaceWorker_YellowRoomsPortal : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef def, IntVec3 loc, Rot4 rot,
            Map map, Thing thingToIgnore = null, Thing blueprint = null)
        {
            if (def == null) return true;
            bool onRoomsMap = map != null && map.Parent is YellowRoomsMapParent;

            if (def.defName == "YellowRooms_PortalFromRooms")
            {
                if (!onRoomsMap)
                    return new AcceptanceReport("YellowRooms_PlaceFromRoomsOnlyInRooms".Translate());
            }
            else if (def.defName == "YellowRooms_PortalToRooms")
            {
                if (onRoomsMap)
                    return new AcceptanceReport("YellowRooms_PlaceToRoomsNotInRooms".Translate());
            }
            return true;
        }
    }
}
