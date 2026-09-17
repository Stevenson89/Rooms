using RimWorld.Planet;
using Verse;

namespace arsiy.Rooms
{
    public class YellowRoomsWorldComponent : WorldComponent
    {
        public bool hasVisitedYellowRooms;

        public YellowRoomsWorldComponent(World world) : base(world) {}

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref hasVisitedYellowRooms, "hasVisitedYellowRooms", false);
        }

        public static bool HasVisited
        {
            get
            {
                var comp = Find.World?.GetComponent<YellowRoomsWorldComponent>();
                return comp?.hasVisitedYellowRooms ?? false;
            }
            set
            {
                var comp = Find.World?.GetComponent<YellowRoomsWorldComponent>();
                if (comp != null) comp.hasVisitedYellowRooms = value;
            }
        }
    }
}