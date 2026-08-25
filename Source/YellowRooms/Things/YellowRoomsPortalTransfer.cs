using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace arsiy.Rooms.Things
{
    
    
    
    
    
    
    public interface IYellowRoomsPortalCargo
    {
        Thing PortalThing { get; }
        Map SourceMap { get; }
        List<TransferableOneWay> LeftToLoad { get; }
        List<Pawn> BoardingPawns { get; }

        
        bool HasItemsPending { get; }
        
        bool Loading { get; }

        Map DestinationMap { get; set; }
        IntVec3 DestinationCell { get; set; }
        ThingOwner CargoContainer { get; }

        bool CanUsePortal(out string reason);
        string EnterLabel { get; }
        string EnterDesc { get; }

        bool AcceptTransferSelection(List<TransferableOneWay> items, List<Pawn> pawns, Pawn driver);
        void OnItemDeposited_Internal(Thing item);
    }

    
    
    
    
    
    public class YellowRoomsPortalContainer : ThingOwner
    {
        public IYellowRoomsPortalCargo portal;

        public override int Count => 0;

        public override int TryAdd(Thing item, int count, bool canMergeWithExistingStacks = true)
        {
            if (TryAdd(item, canMergeWithExistingStacks))
                return count;
            return 0;
        }

        public override bool TryAdd(Thing item, bool canMergeWithExistingStacks = true)
        {
            Map m = portal?.DestinationMap;
            if (m == null || item == null || item.stackCount <= 0) return false;
            portal.OnItemDeposited_Internal(item);
            IntVec3 cell = portal.DestinationCell;
            if (!cell.IsValid) cell = portal.PortalThing?.Position ?? IntVec3.Invalid;
            GenDrop.TryDropSpawn(item, cell, m, ThingPlaceMode.Near, out _);
            return true;
        }

        
        
        public override int GetCountCanAccept(Thing item, bool canMergeWithExistingStacks = true)
        {
            if (item == null || item.stackCount <= 0) return 0;
            if (portal == null || !portal.Loading || portal.DestinationMap == null) return 0;
            return item.stackCount;
        }

        public override int IndexOf(Thing item) => -1;

        public override bool Remove(Thing item) => false;

        protected override Thing GetAt(int index) => null;
    }

    
    
    
    
    
    public static class YellowRoomsPortalTransfer
    {
        
        public static IYellowRoomsPortalCargo CompFor(Thing t)
        {
            if (t == null) return null;
            return t.TryGetComp<Comp_DoorToRooms>() as IYellowRoomsPortalCargo
                ?? t.TryGetComp<Comp_YellowRoomsReturnDoor>() as IYellowRoomsPortalCargo;
        }

        public static void Startup_PlaceContainer(Thing parent) { }

        
        public static void AddToTheToLoadList(IYellowRoomsPortalCargo p, TransferableOneWay t, int count)
        {
            if (p == null || p.LeftToLoad == null || !t.HasAnyThing || count <= 0) return;
            var existing = TransferableUtility.TransferableMatching(t.AnyThing, p.LeftToLoad, TransferAsOneMode.PodsOrCaravanPacking);
            if (existing != null)
            {
                for (int i = 0; i < t.things.Count; i++)
                    if (!existing.things.Contains(t.things[i]))
                        existing.things.Add(t.things[i]);
                if (existing.CanAdjustBy(count).Accepted)
                    existing.AdjustBy(count);
            }
            else
            {
                var added = new TransferableOneWay();
                added.things.AddRange(t.things);
                added.AdjustTo(count);
                p.LeftToLoad.Add(added);
            }
        }

        public static int SubtractFromTheToLoadList(IYellowRoomsPortalCargo p, Thing t, int count)
        {
            if (p == null || p.LeftToLoad == null) return 0;
            var tt = TransferableUtility.TransferableMatchingDesperate(t, p.LeftToLoad, TransferAsOneMode.PodsOrCaravanPacking);
            if (tt == null || tt.CountToTransfer <= 0) return 0;
            int num = Mathf.Min(count, tt.CountToTransfer);
            tt.AdjustBy(-num);
            tt.things.Remove(t);
            if (tt.CountToTransfer <= 0)
                p.LeftToLoad.Remove(tt);
            return num;
        }

        
        private static readonly HashSet<Thing> neededThings = new HashSet<Thing>();
        private static readonly Dictionary<TransferableOneWay, int> tmpLoading = new Dictionary<TransferableOneWay, int>();

        public static ThingCount FindThingToLoad(Pawn pawn, IYellowRoomsPortalCargo p)
        {
            if (p == null) return default;
            neededThings.Clear();
            tmpLoading.Clear();
            var left = p.LeftToLoad;
            if (left != null)
            {
                var list = p.SourceMap.mapPawns.PawnsInFaction(pawn.Faction);
                for (int i = 0; i < list.Count; i++)
                {
                    var other = list[i];
                    if (other == pawn || other.CurJobDef != JobDefOf_Rooms.CarryToYellowRoomsPortal) continue;
                    var drv = other.jobs.curDriver as JobDriver_CarryToYellowRoomsPortal;
                    if (drv == null || drv.Container != p.PortalThing) continue;
                    var tt = TransferableUtility.TransferableMatchingDesperate(drv.ThingToCarry, left, TransferAsOneMode.PodsOrCaravanPacking);
                    if (tt != null)
                        tmpLoading[tt] = tmpLoading.TryGetValue(tt, out var v) ? v + drv.initialCount : drv.initialCount;
                }
                for (int j = 0; j < left.Count; j++)
                {
                    var tow = left[j];
                    if (tow == null) continue;
                    int val = tmpLoading.TryGetValue(tow, out var v2) ? v2 : 0;
                    if (tow.CountToTransfer - val > 0)
                        for (int k = 0; k < tow.things.Count; k++)
                            neededThings.Add(tow.things[k]);
                }
            }
            if (neededThings.Count == 0)
            {
                tmpLoading.Clear();
                return default;
            }
            Thing thing = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map,
                ThingRequest.ForGroup(ThingRequestGroup.HaulableEver),
                PathEndMode.Touch, TraverseParms.For(pawn), 9999f,
                x => neededThings.Contains(x) && pawn.CanReserve(x) && !x.IsForbidden(pawn) && pawn.carryTracker.AvailableStackSpace(x.def) > 0,
                null, 0, -1, false, RegionType.Set_Passable, false, true);
            if (thing == null)
            {
                
                
                
                
                foreach (Thing neededThing in neededThings)
                {
                    if (neededThing is Pawn toCarry
                        && ((!toCarry.IsColonist && !toCarry.IsColonyMech) || toCarry.Downed || toCarry.IsSelfShutdown())
                        && !toCarry.inventory.UnloadEverything
                        && pawn.CanReserveAndReach(toCarry, PathEndMode.Touch, Danger.Deadly))
                    {
                        neededThings.Clear();
                        tmpLoading.Clear();
                        return new ThingCount(toCarry, 1);
                    }
                }
                neededThings.Clear();
                tmpLoading.Clear();
                return default;
            }
            TransferableOneWay tt2 = null;
            for (int idx = 0; idx < left.Count; idx++)
                if (left[idx].things.Contains(thing)) { tt2 = left[idx]; break; }
            int already = tmpLoading.TryGetValue(tt2, out var v3) ? v3 : 0;
            int toLoad = tt2 == null ? thing.stackCount : Mathf.Min(tt2.CountToTransfer - already, thing.stackCount);
            tmpLoading.Clear();
            if (toLoad <= 0) return default;
            return new ThingCount(thing, toLoad);
        }

        
        public static Job MakeCarryJob(Pawn pawn, IYellowRoomsPortalCargo p, bool forced = false)
        {
            if (p == null || !p.HasItemsPending) return null;
            if (!pawn.CanReserve(p.PortalThing, 1, -1, null, forced)) return null;
            var tc = FindThingToLoad(pawn, p);
            if (tc.Thing == null || tc.Count <= 0 || !pawn.CanReserve(tc.Thing, 1, -1, null, forced))
                return null;
            var job = JobMaker.MakeJob(JobDefOf_Rooms.CarryToYellowRoomsPortal, tc.Thing, p.PortalThing);
            job.count = tc.Count;
            job.ignoreForbidden = true;
            return job;
        }

        public static IYellowRoomsPortalCargo CompFromPortalThing(Thing thing) => CompFor(thing);

        
        public static void OnItemDeposited(IYellowRoomsPortalCargo p, Thing item)
        {
            if (p == null || item == null) return;
            if (item is Pawn carriedPawn)
                p.BoardingPawns?.Remove(carriedPawn);
            SubtractFromTheToLoadList(p, item, item.stackCount);
            if (p.LeftToLoad != null && p.LeftToLoad.All(t => t == null || t.CountToTransfer <= 0))
                Messages.Message("YellowRooms_TransferReady".Translate(p.EnterLabel), p.PortalThing, MessageTypeDefOf.TaskCompletion, false);
        }

        public static void Cancel(IYellowRoomsPortalCargo p)
        {
            if (p == null) return;
            p.LeftToLoad?.Clear();
            p.BoardingPawns?.Clear();
            p.DestinationMap = null;
            p.DestinationCell = IntVec3.Invalid;
            if (p.PortalThing != null && p.PortalThing.Spawned)
                Messages.Message("YellowRooms_TransferCancelled".Translate(), p.PortalThing, MessageTypeDefOf.NeutralEvent, false);
        }
    }
}