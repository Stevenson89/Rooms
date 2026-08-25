using RimWorld;
using Verse;
using Verse.AI;

namespace arsiy.Rooms.Things
{
    
    
    
    
    
    
    
    
    
    
    public class JobDriver_CarryToYellowRoomsPortal : JobDriver_HaulToContainer
    {
        private const int DepositDuration = 90;

        public int initialCount;

        public IYellowRoomsPortalCargo Cargo => YellowRoomsPortalTransfer.CompFor(Container);

        protected override int Duration => DepositDuration;

        public override string GetReport()
        {
            Thing thing = pawn.carryTracker?.CarriedThing ?? base.TargetThingA;
            if (thing == null || !job.targetB.HasThing)
                return "ReportHaulingUnknown".Translate();
            return "ReportCarryToYellowRoomsPortal".Translate(thing.Label, job.targetB.Thing.LabelShort.Named("DESTINATION"), thing.Named("THING"));
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref initialCount, "initialCount", 0);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            pawn.ReserveAsManyAsPossible(job.GetTargetQueue(TargetIndex.A), job);
            pawn.ReserveAsManyAsPossible(job.GetTargetQueue(TargetIndex.B), job);
            return true;
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();
            ThingCount thingCount = (!job.targetA.IsValid)
                ? YellowRoomsPortalTransfer.FindThingToLoad(pawn, Cargo)
                : new ThingCount(job.targetA.Thing, job.targetA.Thing.stackCount);
            if (job.playerForced && pawn.carryTracker.CarriedThing != null && pawn.carryTracker.CarriedThing != thingCount.Thing)
            {
                pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);
            }
            if (thingCount.Thing == null || thingCount.Count <= 0)
            {
                pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                return;
            }
            job.targetA = thingCount.Thing;
            job.count = thingCount.Count;
            initialCount = thingCount.Count;
            pawn.Reserve(thingCount.Thing, job);
        }
    }
}
