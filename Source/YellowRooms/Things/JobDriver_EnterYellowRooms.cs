using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace arsiy.Rooms.Things
{
    public class JobDriver_EnterYellowRooms : JobDriver
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
                    var comp = TargetThingA.TryGetComp<Comp_DoorToRooms>();
                    if (comp != null)
                    {
                        comp.EnterYellowRooms(pawn);
                        
                        
                        
                        if (comp is Comp_YellowRoomsPortalBase baseComp)
                            baseComp.NotifyPawnTransferred(pawn);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}
