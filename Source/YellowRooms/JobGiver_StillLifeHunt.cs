using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace arsiy.Rooms.YellowRooms
{
    public class JobGiver_StillLifeHunt : ThinkNode_JobGiver
    {
        const float HUNT_THRESHOLD = 0.3f;
        static HediffDef StillLifeDef => DefDatabase<HediffDef>.GetNamed("YellowRooms_StillLife");
        static HediffDef HungryDef => DefDatabase<HediffDef>.GetNamed("YellowRooms_Hungry");
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.health.hediffSet.GetFirstHediffOfDef(HungryDef) == null)
                return null;
            if (pawn.IsPlayerControlled)
                return null;
            Pawn prey = FindAnyPrey(pawn);
            if (prey == null)
                return null;
            Job job = JobMaker.MakeJob(JobDefOf.PredatorHunt, prey);
            job.killIncappedTarget = true;
            return job;
        }

        // Find a suitable edible creature. Prioritizes Humans > Corpses > Still lifes
        Pawn FindAnyPrey(Pawn hunter)
        {
            float foodPercent = hunter.needs.TryGetNeed(NeedDefOf.Food).CurLevelPercentage;
            Pawn nearbyHuman = FindPrey(hunter, true, false); // Human within line of sight
            if (nearbyHuman != null)
                return nearbyHuman;
            if (foodPercent <= HUNT_THRESHOLD)
            {
                Pawn farHuman = FindPrey(hunter, false, false); // Any human on the map
                if (farHuman != null)
                    return farHuman;
            }
            ThingRequest corpseRequest = ThingRequest.ForGroup(ThingRequestGroup.Corpse);
            TraverseParms traverseParms = TraverseParms.For(hunter, Danger.Deadly);
            Corpse closestEdibleCorpse = (Corpse)GenClosest.ClosestThingReachable(
                hunter.Position,
                hunter.Map,
                corpseRequest,
                PathEndMode.Touch,
                traverseParms,
                validator: t => !t.IsNotFresh()
                );
            if (closestEdibleCorpse != null)
                return closestEdibleCorpse.InnerPawn;
            Pawn nearbyStillLife = FindPrey(hunter, true, true); // Still life within line of sight
            if (nearbyStillLife != null)
                return nearbyStillLife;
            if (foodPercent <= HUNT_THRESHOLD)
                return FindPrey(hunter, false, true); // Any still life on the map
            return null;
        }

        Pawn FindPrey(Pawn hunter, bool mustSee, bool allowStillLifes)
        {
            Pawn best = null;
            float bestDistanceSq = float.PositiveInfinity;
            foreach (Pawn p in hunter.Map.mapPawns.AllPawnsSpawned)
            {
                if (p == hunter)
                    continue;
                float distSq = (p.Position - hunter.Position).LengthHorizontalSquared;
                if (distSq >= bestDistanceSq)
                    continue;

                if (!hunter.CanSee(p) && mustSee)
                    continue;
                bool hasStillLifeHediff = p.health.hediffSet.GetFirstHediffOfDef(StillLifeDef) != null;
                if (hasStillLifeHediff == true && !allowStillLifes)
                    continue;
                if (!hunter.CanReserveAndReach(p, PathEndMode.Touch, Danger.Deadly))
                    continue;

                best = p;
                bestDistanceSq = distSq;
            }
            return best;
        }
    }
}
