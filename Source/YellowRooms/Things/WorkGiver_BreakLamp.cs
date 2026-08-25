using RimWorld;
using Verse;
using Verse.AI;

namespace arsiy.Rooms.Things
{
    
    
    
    
    
    
    
    
    
    
    
    
    public class WorkGiver_BreakLamp : WorkGiver_RemoveBuilding
    {
        protected override DesignationDef Designation => arsiy.Rooms.DesignationDefOf_Rooms.YellowRooms_BreakLamp;

        protected override JobDef RemoveBuildingJob => JobDefOf_Rooms.YellowRooms_BreakLamp;
    }
}
