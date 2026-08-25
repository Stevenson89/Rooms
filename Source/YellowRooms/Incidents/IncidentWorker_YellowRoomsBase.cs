using RimWorld;
using Verse;

namespace arsiy.Rooms.Incidents
{
    
    
    
    
    public abstract class IncidentWorker_YellowRoomsBase : IncidentWorker
    {
        protected virtual bool OnlyOnYellowMaps => true;

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (OnlyOnYellowMaps)
            {
                var map = parms.target as Map;
                if (map == null || !YellowRoomsUtility.IsYellowRoomsMap(map))
                    return false;
            }
            return base.CanFireNowSub(parms);
        }
    }
}
