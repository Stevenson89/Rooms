using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace arsiy.Rooms
{
    public class HediffComp_BetrayalTimer : HediffComp
    {
        private int _ticksRemaining;
        private bool _betrayed;
        private static readonly IntRange BetrayDelayTicks = new IntRange(120000, 240000);

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            _ticksRemaining = BetrayDelayTicks.RandomInRange;
            _betrayed = false;
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (_betrayed) return;
            var pawn = parent?.pawn;
            if (pawn == null || pawn.Destroyed || !pawn.Spawned) return;
            if (!pawn.IsHashIntervalTick(2500)) return; 

            _ticksRemaining -= 2500;
            if (_ticksRemaining <= 0)
                Betray();
        }

        private void Betray()
        {
            if (_betrayed) return;
            _betrayed = true;

            var pawn = parent?.pawn;
            if (pawn == null || pawn.Destroyed || !pawn.Spawned) return;

            var faction = YellowRoomsUtility.GetSurvivors();
            if (faction == null) return;

            pawn.SetFaction(faction);

            
            
            if (pawn.apparel != null)
            {
                foreach (var ap in pawn.apparel.WornApparel)
                    ap.SetFaction(faction);
            }
            if (pawn.equipment?.Primary != null)
                pawn.equipment.Primary.SetFaction(faction);

            var map = pawn.Map;
            if (map != null)
            {
                LordMaker.MakeNewLord(faction,
                    new LordJob_AssaultColony(faction, true, true, false, true),
                    map, new List<Pawn> { pawn });
            }

            var letter = LetterMaker.MakeLetter("YellowRooms_Letter_Betrayal_Title".Translate(),
                "YellowRooms_Letter_Betrayal_Text".Translate(pawn.LabelShort),
                LetterDefOf.ThreatBig, pawn);
            Find.LetterStack.ReceiveLetter(letter);
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref _ticksRemaining, "ticksRemaining", 0);
            Scribe_Values.Look(ref _betrayed, "betrayed", false);
        }
    }

    public class HediffCompProperties_BetrayalTimer : HediffCompProperties
    {
        public HediffCompProperties_BetrayalTimer()
        {
            compClass = typeof(HediffComp_BetrayalTimer);
        }
    }
}
