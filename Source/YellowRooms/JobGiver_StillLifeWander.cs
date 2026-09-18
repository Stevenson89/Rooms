using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace arsiy.Rooms.YellowRooms
{
    public class JobGiver_StillLifeWander : JobGiver_WanderAnywhere
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.health.hediffSet.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamed("YellowRooms_StillLife")) != null && !pawn.IsPlayerControlled)
                return base.TryGiveJob(pawn);
            return null;
        }
    }
}
