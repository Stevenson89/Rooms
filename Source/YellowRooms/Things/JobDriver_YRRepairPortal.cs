using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace arsiy.Rooms.Things
{
    
    
    
    
    
    
    
    
    
    public class JobDriver_YRRepairPortal : JobDriver
    {
        public const int SteelNeeded = 80;
        public const int SpacerNeeded = 2;

        private const int WorkTicks = 1200;

        private int steelRemaining = SteelNeeded;
        private int spacerRemaining = SpacerNeeded;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);

            
            yield return Toils_General.DoAtomic(delegate
            {
                if (!WorkGiver_YRRepairPortal.PortalIsBroken(TargetThingA))
                {
                    EndJobWith(JobCondition.Succeeded);
                    return;
                }
                
                
                
                
                steelRemaining -= WorkGiver_YRRepairPortal.CountDeliveredNear(TargetThingA, ThingDefOf.Steel);
                spacerRemaining -= WorkGiver_YRRepairPortal.CountDeliveredNear(TargetThingA, ThingDefOf.ComponentSpacer);
                if (steelRemaining < 0) steelRemaining = 0;
                if (spacerRemaining < 0) spacerRemaining = 0;
            });

            
            Toil resolveSteel = ToilMaker.MakeToil("ResolveSteel");
            resolveSteel.initAction = delegate
            {
                if (steelRemaining <= 0)
                {
                    ReadyForNextToil();
                    return;
                }
                var th = FindResource(ThingDefOf.Steel);
                if (th == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                job.SetTarget(TargetIndex.B, th);
            };
            resolveSteel.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return resolveSteel;

            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch)
                .FailOnDespawnedOrNull(TargetIndex.B);

            yield return Toils_General.DoAtomic(delegate
            {
                int avail = pawn.carryTracker.AvailableStackSpace(ThingDefOf.Steel);
                if (avail <= 0 || steelRemaining <= 0)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                job.count = Mathf.Min(steelRemaining, avail);
            });

            yield return Toils_Haul.StartCarryThing(TargetIndex.B, reserve: false);
            yield return Toils_Haul.CarryHauledThingToCell(TargetIndex.A, PathEndMode.Touch);

            yield return Toils_General.DoAtomic(delegate
            {
                var carried = pawn.carryTracker.CarriedThing;
                if (carried == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                steelRemaining -= carried.stackCount;
                pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out var _);
            });

            yield return Toils_Jump.JumpIf(resolveSteel, () => steelRemaining > 0);

            
            Toil resolveSpacer = ToilMaker.MakeToil("ResolveSpacer");
            resolveSpacer.initAction = delegate
            {
                if (spacerRemaining <= 0)
                {
                    ReadyForNextToil();
                    return;
                }
                var th = FindResource(ThingDefOf.ComponentSpacer);
                if (th == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                job.SetTarget(TargetIndex.C, th);
            };
            resolveSpacer.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return resolveSpacer;

            yield return Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.ClosestTouch)
                .FailOnDespawnedOrNull(TargetIndex.C);

            yield return Toils_General.DoAtomic(delegate
            {
                int avail = pawn.carryTracker.AvailableStackSpace(ThingDefOf.ComponentSpacer);
                if (avail <= 0 || spacerRemaining <= 0)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                job.count = Mathf.Min(spacerRemaining, avail);
            });

            yield return Toils_Haul.StartCarryThing(TargetIndex.C, reserve: false);
            yield return Toils_Haul.CarryHauledThingToCell(TargetIndex.A, PathEndMode.Touch);

            yield return Toils_General.DoAtomic(delegate
            {
                var carried = pawn.carryTracker.CarriedThing;
                if (carried == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                spacerRemaining -= carried.stackCount;
                pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out var _);
            });

            yield return Toils_Jump.JumpIf(resolveSpacer, () => spacerRemaining > 0);

            
            Toil work = Toils_General.Wait(WorkTicks, TargetIndex.A);
            work.FailOnDespawnedOrNull(TargetIndex.A);
            work.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            work.WithProgressBarToilDelay(TargetIndex.A);
            work.activeSkill = () => SkillDefOf.Construction;
            if (TargetThingA?.def?.repairEffect != null)
                work.WithEffect(TargetThingA.def.repairEffect, TargetIndex.A);
            yield return work;

            
            yield return new Toil
            {
                initAction = delegate
                {
                    ConsumeDroppedMaterials();
                    NotifyPortalRepaired();
                    ClearRepairDesignation();
                    Messages.Message("YellowRooms_PortalRepaired".Translate(), TargetThingA, MessageTypeDefOf.PositiveEvent, false);
                    EndJobWith(JobCondition.Succeeded);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }

        private void ConsumeDroppedMaterials()
        {
            var map = pawn.Map;
            if (map == null || TargetThingA == null || TargetThingA.Map == null) return;

            
            
            
            var things = new List<Thing>();
            foreach (var cell in GenRadial.RadialCellsAround(TargetThingA.Position, 3f, true))
            {
                if (!cell.InBounds(map)) continue;
                var atCell = map.thingGrid.ThingsListAt(cell);
                for (int i = 0; i < atCell.Count; i++)
                {
                    var th = atCell[i];
                    if (th != null && th.Spawned && !th.Destroyed)
                        things.Add(th);
                }
            }

            int steelLeft = SteelNeeded, spacerLeft = SpacerNeeded;
            foreach (var th in things)
            {
                if (th == null || th.Destroyed) continue;
                if (th.def == ThingDefOf.Steel && steelLeft > 0)
                {
                    int n = Mathf.Min(steelLeft, th.stackCount);
                    th.SplitOff(n).Destroy();
                    steelLeft -= n;
                }
                else if (th.def == ThingDefOf.ComponentSpacer && spacerLeft > 0)
                {
                    int n = Mathf.Min(spacerLeft, th.stackCount);
                    th.SplitOff(n).Destroy();
                    spacerLeft -= n;
                }
            }
            if (steelLeft > 0 || spacerLeft > 0)
                RoomsLog.Warning($"[Rooms] Portal repair consumed fewer materials than required (steel {SteelNeeded - steelLeft}/{SteelNeeded}, spacers {SpacerNeeded - spacerLeft}/{SpacerNeeded}). The rest was likely hauled away during the job.");
        }

        private void NotifyPortalRepaired()
        {
            if (TargetThingA == null) return;
            var dc = TargetThingA.TryGetComp<Comp_DoorToRooms>();
            if (dc != null)
            {
                dc.Notify_Repaired();
                return;
            }
            TargetThingA.TryGetComp<Comp_YellowRoomsReturnDoor>()?.Notify_Repaired();
        }

        private void ClearRepairDesignation()
        {
            if (TargetThingA?.Map == null) return;
            TargetThingA.Map.designationManager.TryRemoveDesignationOn(TargetThingA,
                arsiy.Rooms.DesignationDefOf_Rooms.YellowRooms_RepairPortal);
        }

        
        private Thing FindResource(ThingDef def)
        {
            Thing best = null;
            float bestDistSq = float.MaxValue;
            foreach (var th in pawn.Map.listerThings.ThingsOfDef(def))
            {
                if (th == null || !th.Spawned || th.Destroyed || th.stackCount <= 0) continue;
                if (th.IsForbidden(pawn) || th.IsBurning()) continue;
                float d = (th.Position - TargetThingA.Position).LengthHorizontalSquared;
                if (d < bestDistSq)
                {
                    bestDistSq = d;
                    best = th;
                }
            }
            return best;
        }
    }
}
