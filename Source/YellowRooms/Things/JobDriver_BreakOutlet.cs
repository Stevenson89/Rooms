using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace arsiy.Rooms.Things
{
    public class JobDriver_BreakOutlet : JobDriver
    {
        private const int BreakTicks = 60;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            Toil breakWait = Toils_General.Wait(BreakTicks, TargetIndex.A);
            breakWait.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            breakWait.WithProgressBarToilDelay(TargetIndex.A);
            yield return breakWait;

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
