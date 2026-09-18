using System.Collections.Generic;
using RimWorld;
using Verse;

namespace arsiy.Rooms
{
    
    
    
    
    
    public class HediffComp_NeedSuppressor : HediffComp
    {
        
        private static readonly HashSet<string> SuppressedNeedDefNames = new HashSet<string>
        {
            "Rest", "Joy", "Room", "Outdoor", "Beauty", "Comfort"
        };

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            var pawn = parent?.pawn;
            if (pawn == null || pawn.needs == null) return;
            
            if (!pawn.IsHashIntervalTick(60)) return;

            var needs = pawn.needs.AllNeeds;
            for (int i = 0; i < needs.Count; i++)
            {
                var n = needs[i];
                if (n == null) continue;
                if (!SuppressedNeedDefNames.Contains(n.def.defName)) continue;
                n.CurLevel = 1f;
            }
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            
            
            var pawn = parent?.pawn;
            if (pawn == null || pawn.needs == null) return;
            var needs = pawn.needs.AllNeeds;
            for (int i = 0; i < needs.Count; i++)
            {
                var n = needs[i];
                if (n == null) continue;
                if (!SuppressedNeedDefNames.Contains(n.def.defName)) continue;
                n.CurLevel = 0.5f;
            }
        }
    }

    public class HediffCompProperties_NeedSuppressor : HediffCompProperties
    {
        public HediffCompProperties_NeedSuppressor()
        {
            compClass = typeof(HediffComp_NeedSuppressor);
        }
    }
}
