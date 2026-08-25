using System.Linq;
using RimWorld;
using Verse;

namespace arsiy.Rooms
{
    public class StrangePassageScheduler : GameComponent
    {
        private const string IncidentDefName = "YellowRooms_StrangePassage";

        private const string DoorDefName = "YellowRooms_DoorToRooms";

        private const int MinFireDay = 10;

        private const int MaxFireDay = 15;

        private const int FireRetryWindowTicks = GenDate.TicksPerDay * 5;

        private int fireTick = -1;

        private bool queued;

        public StrangePassageScheduler(Game game)
        {
        }

        public override void GameComponentTick()
        {
            if (queued)
            {
                return;
            }
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
            var doorDef = ThingDef.Named(DoorDefName);
            if (def == null || doorDef == null)
            {
                queued = true;
                return;
            }

            Map map = Find.Maps
                .FirstOrDefault(m => m.IsPlayerHome && !YellowRoomsUtility.IsYellowRoomsMap(m));
            if (map == null)
            {
                return;
            }

            if (map.listerThings.ThingsOfDef(doorDef).Any())
            {
                queued = true;
                return;
            }

            var parms = StorytellerUtility.DefaultParmsNow(def.category, map);
            Find.Storyteller.incidentQueue.Add(
                def,
                Find.TickManager.TicksGame,
                parms,
                FireRetryWindowTicks);
            queued = true;
            RoomsLog.Message("StrangePassage scheduled (tick " + fireTick + ")");
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref fireTick, "fireTick", -1);
            Scribe_Values.Look(ref queued, "queued", false);
        }
    }
}