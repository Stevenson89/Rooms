using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace arsiy.Rooms.Things
{
    
    
    
    
    
    
    
    [StaticConstructorOnStartup]
    public class Comp_Pit : ThingComp, IYellowRoomsPitCargo, IThingHolder
    {
        private class PendingRevenge : IExposable
        {
            public Pawn clone;
            public int dueTick;

            public PendingRevenge() { }

            public void ExposeData()
            {
                Scribe_Deep.Look(ref clone, "clone");
                Scribe_Values.Look(ref dueTick, "dueTick", 0);
            }
        }

        private List<TransferableOneWay> cargoLeftToLoad;
        private List<Pawn> cargoBoardingPawns;
        private YellowRoomsPitContainer cargoContainer;
        private List<PendingRevenge> pendingRevenge = new List<PendingRevenge>();

        

        public Thing PitThing => parent;

        public Map SourceMap => parent?.Map;

        public List<TransferableOneWay> LeftToLoad => cargoLeftToLoad;

        public List<Pawn> BoardingPawns => cargoBoardingPawns;

        public bool HasItemsPending =>
            cargoLeftToLoad != null && cargoLeftToLoad.Any(t => t != null && t.CountToTransfer > 0);

        public bool Loading => HasItemsPending || (cargoBoardingPawns != null && cargoBoardingPawns.Count > 0);

        public bool ReadyToThrow => !HasItemsPending && cargoBoardingPawns != null && cargoBoardingPawns.Count > 0;

        public ThingOwner CargoContainer => cargoContainer;

        public bool CanUsePit(out string reason)
        {
            reason = null;
            return true;
        }

        public string EnterLabel => "YellowRooms_PitTitle".Translate();

        public string EnterDesc => "YellowRooms_PitDesc".Translate();

        public bool AcceptTransferSelection(List<TransferableOneWay> items, List<Pawn> pawns, Pawn driver)
        {
            if (!CanUsePit(out var reason))
            {
                Messages.Message(reason, parent, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            cargoLeftToLoad = new List<TransferableOneWay>();
            if (items != null)
            {
                foreach (var t in items)
                {
                    if (t != null && t.CountToTransfer > 0)
                        YellowRoomsPitTransfer.AddToTheToLoadList(this, t, t.CountToTransfer);
                }
            }
            cargoBoardingPawns = pawns?.Where(p => p != null).ToList() ?? new List<Pawn>();
            return true;
        }

        public void RunThrowing(Pawn driver)
        {
            var pawns = YellowRoomsPitTransfer.ThrowingPawns(this);
            foreach (var pawn in pawns)
            {
                var job = JobMaker.MakeJob(JobDefOf_Rooms.EnterPit, parent);
                pawn.jobs?.TryTakeOrderedJob(job, JobTag.Misc);
            }
        }

        public void NotifyItemDelivered(Thing item)
        {
            YellowRoomsPitTransfer.OnItemDelivered(this, item);
        }

        public void NotifyPawnThrown(Pawn pawn)
        {
            if (cargoBoardingPawns != null)
                cargoBoardingPawns.RemoveAll(p => p == null || p == pawn || p.Destroyed);
            YellowRoomsPitTransfer.ThrowPawn(this, pawn);
        }

        

        public void ScheduleRevengeClone(Pawn clone)
        {
            if (clone == null || clone.Destroyed) return;
            clone.DeSpawn();
            int delay = 2 * GenDate.TicksPerDay + Rand.Range(0, 3 * GenDate.TicksPerDay);
            pendingRevenge ??= new List<PendingRevenge>();
            pendingRevenge.Add(new PendingRevenge { clone = clone, dueTick = Find.TickManager?.TicksGame + delay ?? 0 });
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            if (pendingRevenge == null || pendingRevenge.Count == 0) return;
            int now = Find.TickManager?.TicksGame ?? 0;
            for (int i = pendingRevenge.Count - 1; i >= 0; i--)
            {
                var entry = pendingRevenge[i];
                if (entry.clone == null || entry.clone.Destroyed)
                {
                    pendingRevenge.RemoveAt(i);
                    continue;
                }
                if (now >= entry.dueTick)
                {
                    SpawnRevengeClone(entry.clone);
                    pendingRevenge.RemoveAt(i);
                }
            }
        }

        private void SpawnRevengeClone(Pawn clone)
        {
            Map map = parent?.Map;
            if (map == null || clone == null || clone.Destroyed)
            {
                clone?.Destroy();
                return;
            }
            IntVec3 cell = CellFinder.RandomClosewalkCellNear(parent.Position, map, 2);
            GenSpawn.Spawn(clone, cell, map, Rot4.Random);

            var faction = clone.Faction;
            if (faction != null)
            {
                var lordJob = new LordJob_AssaultColony(faction, canKidnap: true, canTimeoutOrFlee: true, sappers: false, useAvoidGridSmart: true);
                LordMaker.MakeNewLord(faction, lordJob, map, new List<Pawn> { clone });
            }

            Find.LetterStack.ReceiveLetter(LetterMaker.MakeLetter(
                "YellowRooms_Letter_PitRevenge_Title".Translate(),
                "YellowRooms_Letter_PitRevenge_Text".Translate(clone.LabelShort),
                LetterDefOf.ThreatBig, clone));

            Messages.Message("YellowRooms_PitClotLurks".Translate(), parent, MessageTypeDefOf.NegativeEvent, false);
        }

        
        
        
        

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return cargoContainer;
        }

        

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            cargoContainer = new YellowRoomsPitContainer { pit = this };
        }

        public override string CompInspectStringExtra()
        {
            string text = base.CompInspectStringExtra();
            if (parent == null || !parent.Spawned) return text;
            if (HasItemsPending)
            {
                int left = 0;
                foreach (var t in cargoLeftToLoad)
                    if (t != null) left += t.CountToTransfer;
                text = text.NullOrEmpty()
                    ? "YellowRooms_CargoLeft".Translate(left)
                    : text + "\n" + "YellowRooms_CargoLeft".Translate(left);
            }
            else if (ReadyToThrow)
            {
                int n = cargoBoardingPawns?.Count(p => p != null) ?? 0;
                text = text.NullOrEmpty()
                    ? "YellowRooms_PitPawnsReady".Translate(n)
                    : text + "\n" + "YellowRooms_PitPawnsReady".Translate(n);
            }
            return text;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra())
                yield return g;

            if (parent == null || !parent.Spawned)
                yield break;

            var throwIn = new Command_Action
            {
                defaultLabel = "YellowRooms_PitGizmo".Translate(),
                defaultDesc = EnterDesc,
                icon = ThrowIcon,
                action = () => Find.WindowStack.Add(new Dialog_PitTransfer(this))
            };
            throwIn.Disabled = !CanUsePit(out var reason);
            throwIn.disabledReason = reason;
            yield return throwIn;

            if (Loading)
            {
                yield return new Command_Action
                {
                    defaultLabel = "YellowRooms_CargoCancel".Translate(),
                    defaultDesc = "YellowRooms_CargoCancelDesc".Translate(),
                    icon = CancelIcon,
                    action = () => YellowRoomsPitTransfer.Cancel(this)
                };
            }

            if (ReadyToThrow)
            {
                yield return new Command_Action
                {
                    defaultLabel = "YellowRooms_PitThrow".Translate(),
                    defaultDesc = "YellowRooms_PitThrowDesc".Translate(),
                    icon = ThrowIcon,
                    action = () => RunThrowing(null)
                };
            }
        }

        private static readonly Texture2D ThrowIcon =
            ContentFinder<Texture2D>.Get("UI/Commands/YellowRooms_PitThrow", reportFailure: false)
            ?? TexCommand.HoldOpen;

        private static readonly Texture2D CancelIcon =
            ContentFinder<Texture2D>.Get("UI/Commands/YellowRooms_PitCancel", reportFailure: false)
            ?? TexCommand.HoldOpen;

        

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref cargoLeftToLoad, "YellowRoomsPitLeft", LookMode.Deep);
            Scribe_Collections.Look(ref cargoBoardingPawns, "YellowRoomsPitPawns", LookMode.Reference);
            Scribe_Collections.Look(ref pendingRevenge, "YellowRoomsPitRevenge", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                cargoLeftToLoad?.RemoveAll(t => t == null || t.AnyThing == null);
                pendingRevenge?.RemoveAll(r => r == null || r.clone == null || r.clone.Destroyed);
            }
        }
    }
}