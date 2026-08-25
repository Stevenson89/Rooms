using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace arsiy.Rooms.Things
{
    public class JobDriver_BreakOutlet : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            yield return new Toil
            {
                initAction = () =>
                {
                    var comp = TargetThingA.TryGetComp<Comp_WallOutlet>();
                    comp?.DoBreak();
                    ClearBreakDesignation(TargetThingA);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }

        private static void ClearBreakDesignation(Thing t)
        {
            if (t?.Map == null) return;
            t.Map.designationManager.TryRemoveDesignationOn(t, arsiy.Rooms.DesignationDefOf_Rooms.YellowRooms_BreakOutlet);
        }
    }
}
