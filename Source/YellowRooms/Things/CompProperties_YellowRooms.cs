using RimWorld;
using Verse;

namespace arsiy.Rooms.Things
{
    public class CompProperties_DoorToRooms : CompProperties
    {
        public CompProperties_DoorToRooms()
        {
            compClass = typeof(Comp_DoorToRooms);
        }
    }

    public class CompProperties_YellowRoomsReturnDoor : CompProperties
    {
        public CompProperties_YellowRoomsReturnDoor()
        {
            compClass = typeof(Comp_YellowRoomsReturnDoor);
        }
    }

    
    public class CompProperties_CeilingLamp : CompProperties
    {
        public CompProperties_CeilingLamp()
        {
            compClass = typeof(Comp_CeilingLamp);
        }
    }

    
    public class CompProperties_WallOutlet : CompProperties
    {
        public CompProperties_WallOutlet()
        {
            compClass = typeof(Comp_WallOutlet);
        }
    }

    
    
    
    
    
    
    public class CompProperties_Pit : CompProperties
    {
        public CompProperties_Pit()
        {
            compClass = typeof(Comp_Pit);
        }
    }
}
