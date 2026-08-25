using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace arsiy.Rooms.Things
{
    
    
    
    
    
    
    
    
    public interface IYellowRoomsPitCargo
    {
        Thing PitThing { get; }
        Map SourceMap { get; }
        List<TransferableOneWay> LeftToLoad { get; }
        List<Pawn> BoardingPawns { get; }

        
        bool HasItemsPending { get; }
        
        bool Loading { get; }
        
        bool ReadyToThrow { get; }

        ThingOwner CargoContainer { get; }

        bool CanUsePit(out string reason);
        string EnterLabel { get; }
        string EnterDesc { get; }

        bool AcceptTransferSelection(List<TransferableOneWay> items, List<Pawn> pawns, Pawn driver);
        void RunThrowing(Pawn driver);
        void NotifyItemDelivered(Thing item);
        void NotifyPawnThrown(Pawn pawn);
    }

    
    
    
    
    
    public class YellowRoomsPitContainer : ThingOwner
    {
        public IYellowRoomsPitCargo pit;

        public override int Count => 0;

        public override int TryAdd(Thing item, int count, bool canMergeWithExistingStacks = true)
        {
            if (TryAdd(item, canMergeWithExistingStacks))
                return count;
            return 0;
        }

        public override bool TryAdd(Thing item, bool canMergeWithExistingStacks = true)
        {
            if (item == null || item.stackCount <= 0 || pit == null) return false;
            pit.NotifyItemDelivered(item);
            return true;
        }

        
        
        public override int GetCountCanAccept(Thing item, bool canMergeWithExistingStacks = true)
        {
            if (item == null || item.stackCount <= 0) return 0;
            if (pit == null || !pit.Loading) return 0;
            return item.stackCount;
        }

        public override int IndexOf(Thing item) => -1;

        public override bool Remove(Thing item) => false;

        protected override Thing GetAt(int index) => null;
    }

    
    
    
    
    
    public static class YellowRoomsPitTransfer
    {
        public static IYellowRoomsPitCargo CompFor(Thing t)
        {
            if (t == null) return null;
            return t.TryGetComp<Comp_Pit>() as IYellowRoomsPitCargo;
        }

        

        public static void AddToTheToLoadList(IYellowRoomsPitCargo p, TransferableOneWay t, int count)
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

        private static int SubtractFromTheToLoadList(IYellowRoomsPitCargo p, Thing t, int count)
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

public static ThingCount FindThingToLoad(Pawn pawn, IYellowRoomsPitCargo p)
        {
            if (p == null) return default;
            RoomsLog.Message("[YRPIT] FindThingToLoad enter, map=" + (p.SourceMap != null) + " leftCount=" + (p.LeftToLoad?.Count ?? -1) + " HasItemsPending=" + p.HasItemsPending);
            neededThings.Clear();
            tmpLoading.Clear();
            var left = p.LeftToLoad;
            if (left != null)
            {
                var list = p.SourceMap.mapPawns.PawnsInFaction(pawn.Faction);
                for (int i = 0; i < list.Count; i++)
                {
                    var other = list[i];
                    if (other == pawn || other.CurJobDef != JobDefOf_Rooms.CarryToPit) continue;
                    var drv = other.jobs.curDriver as JobDriver_CarryToPit;
                    if (drv == null || drv.Container != p.PitThing) continue;
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
                RoomsLog.Message("[YRPIT] FindThingToLoad returns null: no needed things (left=" + (p.LeftToLoad?.Count ?? -1) + ")");
                tmpLoading.Clear();
                return default;
            }
Thing thing = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map,
                ThingRequest.ForGroup(ThingRequestGroup.HaulableEver),
                PathEndMode.Touch, TraverseParms.For(pawn), 9999f,
                x => neededThings.Contains(x) && pawn.CanReserve(x) && !x.IsForbidden(pawn) && pawn.carryTracker.AvailableStackSpace(x.def) > 0,
                null, 0, -1, false, RegionType.Set_Passable, false, true);
            neededThings.Clear();
            if (thing == null)
            {
                RoomsLog.Message("[YRPIT] FindThingToLoad returns null: ClosestThingReachable none");
                tmpLoading.Clear();
                return default;
            }
            TransferableOneWay tt2 = null;
            for (int idx = 0; idx < left.Count; idx++)
                if (left[idx].things.Contains(thing)) { tt2 = left[idx]; break; }
            int already = tmpLoading.TryGetValue(tt2, out var v3) ? v3 : 0;
            int toLoad = tt2 == null ? thing.stackCount : Mathf.Min(tt2.CountToTransfer - already, thing.stackCount);
            tmpLoading.Clear();
            if (toLoad <= 0) { RoomsLog.Message("[YRPIT] FindThingToLoad returns null: toLoad<=0"); return default; }
            RoomsLog.Message("[YRPIT] FindThingToLoad OK: thing=" + thing + " toLoad=" + toLoad);
            return new ThingCount(thing, toLoad);
        }

        public static Job MakeCarryJob(Pawn pawn, IYellowRoomsPitCargo p, bool forced = false)
        {
            if (p == null) { RoomsLog.Message("[YRPIT] MakeCarryJob null: p"); return null; }
            if (!p.HasItemsPending) { RoomsLog.Message("[YRPIT] MakeCarryJob null: no pending"); return null; }
            if (!pawn.CanReserve(p.PitThing, 1, -1, null, forced)) { RoomsLog.Message("[YRPIT] MakeCarryJob null: cannot reserve pit " + p.PitThing); return null; }
            var tc = FindThingToLoad(pawn, p);
            if (tc.Thing == null || tc.Count <= 0) { RoomsLog.Message("[YRPIT] MakeCarryJob null: no thing to load"); return null; }
            if (!pawn.CanReserve(tc.Thing, 1, -1, null, forced)) { RoomsLog.Message("[YRPIT] MakeCarryJob null: cannot reserve thing " + tc.Thing); return null; }
            var job = JobMaker.MakeJob(JobDefOf_Rooms.CarryToPit, tc.Thing, p.PitThing);
            job.count = tc.Count;
            job.ignoreForbidden = true;
            RoomsLog.Message("[YRPIT] MakeCarryJob OK: job=" + job + " thing=" + tc.Thing + " count=" + tc.Count + " pawn=" + pawn);
            return job;
        }

        

        
        public static List<Pawn> ThrowingPawns(IYellowRoomsPitCargo p)
        {
            if (p?.BoardingPawns == null) return new List<Pawn>();
            return p.BoardingPawns.Where(x => x != null && !x.Destroyed && x.Spawned).ToList();
        }

        public static void OnItemDelivered(IYellowRoomsPitCargo p, Thing item)
        {
            if (p == null || item == null) return;
            SubtractFromTheToLoadList(p, item, item.stackCount);
            ThrowItem(p, item);
            if (p.LeftToLoad != null && p.LeftToLoad.All(t => t == null || t.CountToTransfer <= 0))
                Messages.Message("YellowRooms_PitCarried".Translate(p.EnterLabel), p.PitThing, MessageTypeDefOf.TaskCompletion, false);
        }

        public static void Cancel(IYellowRoomsPitCargo p)
        {
            if (p == null) return;
            p.LeftToLoad?.Clear();
            p.BoardingPawns?.Clear();
            if (p.PitThing != null && p.PitThing.Spawned)
                Messages.Message("YellowRooms_TransferCancelled".Translate(), p.PitThing, MessageTypeDefOf.NeutralEvent, false);
        }

        

        
        
        
        
        
        public static void ThrowItem(IYellowRoomsPitCargo p, Thing item)
        {
            if (p == null || item == null || item.Destroyed) return;
            Map map = p.SourceMap ?? item.MapHeld;
            if (map == null)
            {
                item.Destroy();
                return;
            }

            
            
            IntVec3 groundCell = StandableNear(p.PitThing != null ? p.PitThing.Position : item.Position, map, 2);
            Thing flung = item;
            if (!item.Spawned)
                GenDrop.TryDropSpawn(item, groundCell, map, ThingPlaceMode.Near, out flung);

            float roll = Rand.Value;
            if (roll < 0.2f)
            {
                
                flung?.Destroy();
                return;
            }
            if (roll < 0.8f)
            {
                
                DropSimilarItemFromCeiling(item, map, item.Position);
                flung?.Destroy();
                return;
            }
            
            SpawnCrawlerFromPit(p, map);
            if (flung != null) flung.Destroy();
        }

        private static IntVec3 StandableNear(IntVec3 from, Map map, int radius)
        {
            IntVec3 c = CellFinder.RandomClosewalkCellNear(from, map, radius);
            return c.IsValid ? c : from;
        }

        private static bool IsThrowableFallenItem(ThingDef def)
        {
            if (def == null || def.thingClass == null || def.race != null) return false;
            if (def.IsBlueprint || def.IsFrame) return false;
            if (def.thingClass == typeof(MinifiedThing)) return false;
            if (def.category != ThingCategory.Item) return false;
            return def.GetStatValueAbstract(StatDefOf.MarketValue) > 1f;
        }

        
        
        
        
        
        
        
        private static void DropSimilarItemFromCeiling(Thing item, Map map, IntVec3 fallback)
        {
            float targetValue = item.GetStatValue(StatDefOf.MarketValue) * item.stackCount;
            if (targetValue <= 0f) return;

            var pool = DefDatabase<ThingDef>.AllDefsListForReading.Where(IsThrowableFallenItem).ToList();
            if (pool.Count == 0) return;

            float scaled = targetValue * Rand.Range(0.9f, 1.1f);

            
            
            ThingDef chosen = null;
            float bestDiff = float.MaxValue;
            int bestCount = 1;
            for (int i = 0; i < pool.Count; i++)
            {
                float perUnit = pool[i].GetStatValueAbstract(StatDefOf.MarketValue);
                if (perUnit <= 0f) continue;
                int count = Mathf.Clamp(Mathf.RoundToInt(scaled / perUnit), 1, pool[i].stackLimit);
                float diff = Mathf.Abs(count * perUnit - scaled);
                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    bestCount = count;
                    chosen = pool[i];
                }
            }
            if (chosen == null) return;

            Thing newThing = ThingMaker.MakeThing(chosen);
            if (newThing == null) return;
            newThing.stackCount = bestCount;

            IntVec3 cell = CeilingHoleFallCell(map, fallback);
            GenDrop.TryDropSpawn(newThing, cell, map, ThingPlaceMode.Near, out _);

            Find.LetterStack.ReceiveLetter(LetterMaker.MakeLetter(
                "YellowRooms_Letter_FellFromCeiling_Title".Translate(),
                "YellowRooms_Letter_FellFromCeiling_Text".Translate(newThing.Label),
                LetterDefOf.PositiveEvent, new TargetInfo(cell, map)));
        }

        private static IntVec3 CeilingHoleFallCell(Map map, IntVec3 fallback)
        {
            var holes = YellowRoomsUtility.GetCeilingHoles(map);
            if (holes.Count > 0)
            {
                var hole = holes.RandomElement();
                for (int i = 0; i < 8; i++)
                {
                    var c = CellFinder.RandomClosewalkCellNear(hole.Position, map, 3);
                    if (c.InBounds(map) && c.Standable(map)) return c;
                }
                return hole.Position;
            }
            return fallback;
        }

        
        private static void SpawnCrawlerFromPit(IYellowRoomsPitCargo p, Map map)
        {
            var faction = YellowRoomsUtility.GetStillLifeFaction();
            var kind = YellowRoomsUtility.RandomStillLifeKind();
            if (faction == null || kind == null || map == null) return;

            IntVec3 cell = StandableNear(p.PitThing != null ? p.PitThing.Position : map.Center, map, 3);
            var pawn = YellowRoomsUtility.SpawnHostilePawn(kind, faction, cell, map);
            if (pawn == null) return;

            var lordJob = new LordJob_AssaultColony(faction, canKidnap: true, canTimeoutOrFlee: true, sappers: false, useAvoidGridSmart: true);
            LordMaker.MakeNewLord(faction, lordJob, map, new List<Pawn> { pawn });

            Find.LetterStack.ReceiveLetter(LetterMaker.MakeLetter(
                "YellowRooms_Letter_PitCrawler_Title".Translate(),
                "YellowRooms_Letter_PitCrawler_Text".Translate(pawn.LabelShort),
                LetterDefOf.ThreatBig, pawn));
        }

        

        
        
        
        
        
        public static void ThrowPawn(IYellowRoomsPitCargo p, Pawn pawn)
        {
            if (p == null || pawn == null || pawn.Destroyed) return;
            Map map = p.SourceMap ?? pawn.Map;
            if (map == null) return;

            IntVec3 fallback = p.PitThing != null ? p.PitThing.Position : map.Center;
            DropSimilarItemFromCeiling(pawn, map, fallback);

            if (Rand.Value < 0.5f)
            {
                var survivor = arsiy.Rooms.Incidents.CloneHelper.MakeSurvivorClone(pawn);
                pawn.DeSpawn();
                pawn.Destroy();
                if (survivor != null)
                {
                    survivor.DeSpawn();
                    map?.GetComponent<YellowRoomsMapComponent>()?.QueuePitSurvivor(survivor);
                }
                return;
            }

            var faction = YellowRoomsUtility.GetStillLifeFaction();
            var clone = arsiy.Rooms.Incidents.CloneHelper.MakeHostileClone(pawn, faction);
            pawn.DeSpawn();
            pawn.Destroy();
            if (clone != null)
            {
                (p as Comp_Pit)?.ScheduleRevengeClone(clone);
            }
        }
    }
}