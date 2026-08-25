using RimWorld;
using Verse;

namespace arsiy.Rooms.GenSteps
{
    public class LayoutWorker_YellowRoomsBase : LayoutWorker_Structure
    {
        public LayoutWorker_YellowRoomsBase(LayoutDef def) : base(def)
        {
        }

        protected override StructureLayout GetStructureLayout(StructureGenParams parms, CellRect rect)
        {
            return RoomLayoutGenerator.GenerateRandomLayout(parms.sketch, rect,
                minRoomHeight: Def.minRoomHeight,
                minRoomWidth: Def.minRoomWidth,
                areaPrunePercent: 0.25f,
                canRemoveRooms: true,
                generateDoors: false,
                corridor: null,
                corridorExpansion: 2,
                maxMergeRoomsRange: new IntRange(2, 4),
                corridorShapes: CorridorShape.All,
                canDisconnectRooms: false);
        }
    }
}