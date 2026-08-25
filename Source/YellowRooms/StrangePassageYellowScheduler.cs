using System.Linq;
using RimWorld;
using Verse;

namespace arsiy.Rooms
{
    public class StrangePassageYellowScheduler : GameComponent
    {
        private const string IncidentDefName = "YellowRooms_StrangePassageYellow";

        private const int MinFireDay = 5;

        private const int MaxFireDay = 10;

        private const int FireRetryWindowTicks = GenDate.TicksPerDay * 5;

        private const int RetryLaterTicks = GenDate.TicksPerDay * 3;

        private int fireTick = -1;

        public StrangePassageYellowScheduler(Game game)
        {
        }

        public override void GameComponentTick()
        {
            if (fireTick < 0)
            {
                int days = Rand.Range(MinFireDay, MaxFireDay + 1);
                fireTick = Find.TickManager.TicksGame
                    + days * GenDate.TicksPerDay
                    + Rand.Range(0, GenDate.TicksPerDay);
            }
            if (Find.TickManager.TicksGame < fireTick)
            {
                return;
            }

            var def = DefDatabase<IncidentDef>.GetNamedSilentFail(IncidentDefName);
            if (def == null)
            {
                return;
            }

            Map map = Find.Maps
                .FirstOrDefault(m => YellowRoomsUtility.IsYellowRoomsMap(m)
                                     && m.mapPawns.FreeColonistsSpawnedCount > 0
                                     && !HasExitDoor(m));
            if (map == null)
            {
                fireTick = Find.TickManager.TicksGame + RetryLaterTicks;
                return;
            }

            var parms = StorytellerUtility.DefaultParmsNow(def.category, map);
            Find.Storyteller.incidentQueue.Add(
                def,
                Find.TickManager.TicksGame,
                parms,
                FireRetryWindowTicks);
            fireTick = -1;
            RoomsLog.Message("Strange passage (yellow rooms) scheduled (tick " + Find.TickManager.TicksGame + ")");
        }

        private static bool HasExitDoor(Map map)
        {
            var returnDoor = ThingDef.Named("YellowRooms_ReturnDoor");
            if (returnDoor != null && map.listerThings.ThingsOfDef(returnDoor).Any()) return true;
            var portalFromRooms = ThingDef.Named("YellowRooms_PortalFromRooms");
            return portalFromRooms != null && map.listerThings.ThingsOfDef(portalFromRooms).Any();
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref fireTick, "fireTick", -1);
        }
    }
}
