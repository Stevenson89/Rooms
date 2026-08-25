using RimWorld;
using Verse;

namespace arsiy.Rooms.Things
{
    public class WorkGiver_BreakOutlet : WorkGiver_RemoveBuilding
    {
        protected override DesignationDef Designation => arsiy.Rooms.DesignationDefOf_Rooms.YellowRooms_BreakOutlet;

        protected override JobDef RemoveBuildingJob => JobDefOf_Rooms.YellowRooms_BreakOutlet;
    }
}
