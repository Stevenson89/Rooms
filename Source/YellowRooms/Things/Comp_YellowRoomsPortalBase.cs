using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace arsiy.Rooms.Things
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    [StaticConstructorOnStartup]
    public abstract class Comp_YellowRoomsPortalBase : ThingComp, IYellowRoomsPortalCargo, IThingHolder
    {
        protected List<TransferableOneWay> cargoLeftToLoad;
        protected List<Pawn> cargoPawns;
        protected Map cargoDestMap;
        protected IntVec3 cargoDestCell = IntVec3.Invalid;
        protected YellowRoomsPortalContainer cargoContainer;

        

        public Thing PortalThing => parent;

        public Map SourceMap => parent?.Map;

        public List<TransferableOneWay> LeftToLoad => cargoLeftToLoad;

        public List<Pawn> BoardingPawns => cargoPawns;

        public bool HasItemsPending =>
            cargoLeftToLoad != null && cargoLeftToLoad.Any(t => t != null && t.CountToTransfer > 0);

        public bool Loading => HasItemsPending || (cargoPawns != null && cargoPawns.Count > 0);

        public Map DestinationMap
        {
            get => cargoDestMap;
            set => cargoDestMap = value;
        }

        public IntVec3 DestinationCell
        {
            get => cargoDestCell;
            set => cargoDestCell = value;
        }

        public ThingOwner CargoContainer => cargoContainer;

        public abstract bool CanUsePortal(out string reason);

        
        public abstract string EnterLabel { get; }

        public abstract string EnterDesc { get; }

        public bool AcceptTransferSelection(List<TransferableOneWay> items, List<Pawn> pawns, Pawn driver)
        {
            if (!CanUsePortal(out var reason))
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
                        YellowRoomsPortalTransfer.AddToTheToLoadList(this, t, t.CountToTransfer);
                }
            }
            cargoPawns = pawns?.Where(p => p != null).ToList() ?? new List<Pawn>();

            
            
            
            foreach (var p in cargoPawns)
            {
                if (p == null || !p.Spawned || p.Map != parent.Map) continue;
                if (!(p.IsColonist || p.IsColonyMechPlayerControlled)) continue;
                var job = MakeEnterJob(p);
                if (job != null)
                    p.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }

            
            
            
            cargoDestMap = null;
            cargoDestCell = IntVec3.Invalid;
            ResolveDestinationAsync();
            return true;
        }

        public void OnItemDeposited_Internal(Thing item)
        {
            YellowRoomsPortalTransfer.OnItemDeposited(this, item);
        }

        
        
        
        
        
        
        
        public void NotifyPawnTransferred(Pawn pawn)
        {
            if (pawn == null) return;
            cargoPawns?.Remove(pawn);
            YellowRoomsPortalTransfer.SubtractFromTheToLoadList(this, pawn, 1);
        }

        
        
        
        
        
        protected virtual Job MakeEnterJob(Pawn pawn) => null;

        
        
        protected abstract void ResolveDestinationAsync();

        
        
        
        
        

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
            cargoContainer = new YellowRoomsPortalContainer { portal = this };
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
            else if (cargoPawns != null && cargoPawns.Count > 0)
            {
                text = text.NullOrEmpty()
                    ? "YellowRooms_CargoReady".Translate()
                    : text + "\n" + "YellowRooms_CargoReady".Translate();
            }
            return text;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra())
                yield return g;

            if (parent == null || !parent.Spawned || parent.Faction != Faction.OfPlayer)
                yield break;

            
            var load = new Command_Action
            {
                defaultLabel = "YellowRooms_CargoGizmo".Translate(),
                defaultDesc = EnterDesc,
                icon = EnterIcon,
                action = () => Find.WindowStack.Add(new Dialog_YellowRoomsPortal(this))
            };
            load.Disabled = !CanUsePortal(out var reason);
            load.disabledReason = reason;
            yield return load;

            
            if (Loading)
            {
                yield return new Command_Action
                {
                    defaultLabel = "YellowRooms_CargoCancel".Translate(),
                    defaultDesc = "YellowRooms_CargoCancelDesc".Translate(),
                    icon = CancelIcon,
                    action = () => YellowRoomsPortalTransfer.Cancel(this)
                };
            }
        }

        private static readonly Texture2D EnterIcon =
            ContentFinder<Texture2D>.Get("UI/Commands/YellowRooms_PortalTransfer", reportFailure: false)
            ?? TexCommand.HoldOpen;

        private static readonly Texture2D CancelIcon =
            ContentFinder<Texture2D>.Get("UI/Commands/YellowRooms_PortalCancel", reportFailure: false)
            ?? TexCommand.ClearPrioritizedWork;

        

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref cargoLeftToLoad, "YellowRoomsCargoLeftToLoad", LookMode.Deep);
            Scribe_Collections.Look(ref cargoPawns, "YellowRoomsCargoPawns", LookMode.Reference);
            Scribe_References.Look(ref cargoDestMap, "YellowRoomsCargoDestMap");
            Scribe_Values.Look(ref cargoDestCell, "YellowRoomsCargoDestCell", IntVec3.Invalid);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                cargoLeftToLoad?.RemoveAll(t => t == null || t.AnyThing == null);
        }
    }
}
