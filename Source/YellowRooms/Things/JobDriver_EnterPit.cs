using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace arsiy.Rooms.Things
{
    
    
    
    
    
    
    
    public class JobDriver_EnterPit : JobDriver
    {
        private const TargetIndex PitInd = TargetIndex.A;

        public IYellowRoomsPitCargo Pit => YellowRoomsPitTransfer.CompFor(job.GetTarget(PitInd).Thing);

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(PitInd);

            yield return Toils_Goto.GotoThing(PitInd, PathEndMode.Touch);

            yield return new Toil
            {
                initAction = () =>
                {
                    IYellowRoomsPitCargo pit = Pit;
                    pawn.DeSpawnOrDeselect();
                    pit?.NotifyPawnThrown(pawn);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}