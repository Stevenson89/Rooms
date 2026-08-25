using System.Collections.Generic;
using RimWorld;
using Verse;

namespace arsiy.Rooms.Things
{
    public class StrangePassageTimerComp : MapComponent
    {
        private class PendingExpiration : IExposable
        {
            public Thing entrance;
            public int expireTick;

            public PendingExpiration() { }

            public PendingExpiration(Thing entrance, int expireTick)
            {
                this.entrance = entrance;
                this.expireTick = expireTick;
            }

            public void ExposeData()
            {
                Scribe_References.Look(ref entrance, "entrance");
                Scribe_Values.Look(ref expireTick, "expireTick", 0);
            }
        }

        private static readonly string[] ExitDoorDefNames =
        {
            "YellowRooms_ReturnDoor",
            "YellowRooms_PortalFromRooms"
        };

        private List<PendingExpiration> _pending = new List<PendingExpiration>();

        public StrangePassageTimerComp(Map map) : base(map) { }

        public void ScheduleExpiration(Thing entrance, int delayTicks)
        {
            if (entrance == null) return;
            _pending.Add(new PendingExpiration(entrance, Find.TickManager.TicksGame + delayTicks));
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (_pending.Count == 0) return;
            var now = Find.TickManager.TicksGame;
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                var p = _pending[i];
                if (p.entrance == null || p.entrance.Destroyed || !p.entrance.Spawned)
                {
                    _pending.RemoveAt(i);
                    continue;
                }
                if (now >= p.expireTick)
                {
                    Expire(p.entrance);
                    _pending.RemoveAt(i);
                }
            }
        }

        private static void Expire(Thing entrance)
        {
            IntVec3 entranceCell = entrance.Position;
            Map entranceMap = entrance.Map;

            foreach (var exitDefName in ExitDoorDefNames)
            {
                var exitDef = ThingDef.Named(exitDefName);
                if (exitDef == null) continue;
                foreach (var m in Find.Maps)
                {
                    foreach (var d in m.listerThings.ThingsOfDef(exitDef))
                    {
                        var comp = d.TryGetComp<Comp_YellowRoomsReturnDoor>();
                        if (comp != null && comp.IsLinkedTo(entrance) && !d.Destroyed)
                            d.DeSpawn();
                    }
                }
            }

            entrance.DeSpawn();

            if (entranceMap != null)
            {
                var letter = LetterMaker.MakeLetter("StrangePassageClosedLabel".Translate(),
                    "StrangePassageClosedDesc".Translate(), LetterDefOf.NeutralEvent,
                    new TargetInfo(entranceCell, entranceMap));
                Find.LetterStack.ReceiveLetter(letter);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref _pending, "pendingExpirations", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                _pending.RemoveAll(p => p.entrance == null);
        }
    }
}
