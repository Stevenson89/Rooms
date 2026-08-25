using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace arsiy.Rooms.Things
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    [StaticConstructorOnStartup]
    public class Comp_YellowRoomsReturnDoor : Comp_YellowRoomsPortalBase
    {
        private Thing sourceDoor;
        private bool needsRelink;
        private bool everLinked;

        
        
        public bool EverLinked => everLinked;

        
        
        
        public bool IsBroken => everLinked && !needsRelink && !SourceAlive();

        private bool SourceAlive()
        {
            return sourceDoor != null && !sourceDoor.Destroyed && sourceDoor.Map != null;
        }

        public void LinkToSource(Thing door)
        {
            sourceDoor = door;
            if (door != null) everLinked = true;
        }

        
        
        public override string CompInspectStringExtra()
        {
            string text = base.CompInspectStringExtra();
            if (IsBroken && parent != null && parent.Spawned)
                text = AppendRepairInfo(text);
            return text;
        }

        
        
        private string AppendRepairInfo(string text)
        {
            int steel = WorkGiver_YRRepairPortal.CountDeliveredNear(parent, ThingDefOf.Steel);
            int spacer = WorkGiver_YRRepairPortal.CountDeliveredNear(parent, ThingDefOf.ComponentSpacer);
            string info = "YellowRooms_RepairNeeds".Translate(
                              JobDriver_YRRepairPortal.SteelNeeded, JobDriver_YRRepairPortal.SpacerNeeded)
                          + "\n" + "YellowRooms_RepairDelivered".Translate(
                              steel, JobDriver_YRRepairPortal.SteelNeeded,
                              spacer, JobDriver_YRRepairPortal.SpacerNeeded);
            return text.NullOrEmpty() ? info : text + "\n" + info;
        }

        
        public bool IsLinkedTo(Thing door) => sourceDoor == door;

        
        public Thing SourceDoor => sourceDoor;

        public override bool CanUsePortal(out string reason)
        {
            if (IsBroken)
            {
                reason = "YellowRooms_NeedsRepair".Translate();
                return false;
            }
            if (!SourceAlive())
            {
                reason = "YellowRooms_ReturnDoorNoLink".Translate();
                return false;
            }
            reason = "";
            return true;
        }

        public override string EnterLabel => "YellowRooms_CargoReturnTitle".Translate();

        public override string EnterDesc => "YellowRooms_CargoReturnDesc".Translate(parent?.def.label ?? "");

        
        
        protected override Job MakeEnterJob(Pawn pawn) =>
            JobMaker.MakeJob(JobDefOf_Rooms.ReturnFromYellowRooms, parent);

        
        
        protected override void ResolveDestinationAsync()
        {
            if (!SourceAlive())
            {
                Messages.Message("YellowRooms_ReturnDoorNoLink".Translate(), parent, MessageTypeDefOf.RejectInput, false);
                return;
            }
            cargoDestMap = sourceDoor.Map;
            cargoDestCell = sourceDoor.Position;
        }

        
        
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            foreach (var o in base.CompFloatMenuOptions(selPawn))
                yield return o;

            if (selPawn == null || !selPawn.Spawned || !selPawn.IsFreeColonist || selPawn.Map != parent.Map)
                yield break;

            
            
            if (!IsBroken)
            {
                yield return new FloatMenuOption("YellowRooms_Return".Translate(), () =>
                {
                    var job = JobMaker.MakeJob(JobDefOf_Rooms.ReturnFromYellowRooms, parent);
                    selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                })
                {
                    revalidateClickTarget = parent
                };
            }

            
            if (IsBroken)
            {
                var repairDes = arsiy.Rooms.DesignationDefOf_Rooms.YellowRooms_RepairPortal;
                if (repairDes != null && parent.Map != null
                    && parent.Map.designationManager.DesignationOn(parent, repairDes) != null)
                {
                    yield return new FloatMenuOption("YellowRooms_RepairPortal".Translate(), () =>
                    {
                        var job = JobMaker.MakeJob(JobDefOf_Rooms.YellowRooms_RepairPortal, parent);
                        selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    })
                    {
                        revalidateClickTarget = parent
                    };
                }
            }
        }

        
        
        
        
        
        
        
        
        public void ReturnThroughDoor(Pawn pawn)
        {
            if (SourceAlive())
            {
                TeleportToSource(pawn);
                return;
            }

            if (everLinked && !needsRelink)
            {
                Messages.Message("YellowRooms_NeedsRepair".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            
            
            
            
            if (!TryRelink() && !TryRelinkViaNewCamp())
            {
                Messages.Message("YellowRooms_NoEntrancePortal".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            TeleportToSource(pawn);
        }

        
        
        public void Notify_Repaired()
        {
            needsRelink = true;
        }

        
        
        
        
        
        
        
        
        private bool TryRelink()
        {
            var entrance = FindRelinkEntrance();
            if (entrance == null) return false;

            if (entrance.IsBroken || !entrance.EnteredBefore)
                entrance.Notify_Relinked(parent);

            LinkToSource(entrance.parent);
            needsRelink = false;
            return true;
        }

        
        
        
        
        
        
        private bool TryRelinkViaNewCamp()
        {
            var campMap = PortablePortalHelper.CreateSurfaceCamp();
            if (campMap == null) return false;

            var entranceDef = ThingDef.Named("YellowRooms_PortalToRooms")
                              ?? ThingDef.Named("YellowRooms_DoorToRooms");
            if (entranceDef == null) return false;

            if (!TryFindPortalSpawn(campMap, entranceDef, out var spawnCell, out var rot))
                return false;

            var entrance = GenSpawn.Spawn(entranceDef, spawnCell, campMap, rot, WipeMode.Vanish);
            var comp = entrance?.TryGetComp<Comp_DoorToRooms>();
            if (comp == null)
            {
                entrance?.Destroy();
                return false;
            }

            comp.Notify_Relinked(parent);
            LinkToSource(entrance);
            needsRelink = false;
            return true;
        }

        
        
        
        
        
        private static bool TryFindPortalSpawn(Map map, ThingDef def, out IntVec3 cell, out Rot4 rot)
        {
            cell = IntVec3.Invalid;
            rot = Rot4.North;

            var rots = new[] { Rot4.North, Rot4.East, Rot4.South, Rot4.West };
            int first = Rand.Range(0, rots.Length);

            foreach (var c in GenRadial.RadialCellsAround(map.Center, 12f, true))
            {
                if (!c.InBounds(map)) continue;
                for (int i = 0; i < rots.Length; i++)
                {
                    var r = rots[(first + i) % rots.Length];
                    if (!FootprintFits(c, r, def, map)) continue;
                    cell = c;
                    rot = r;
                    return true;
                }
            }
            return false;
        }

        
        
        private static bool FootprintFits(IntVec3 anchor, Rot4 rot, ThingDef def, Map map)
        {
            foreach (var c in GenAdj.OccupiedRect(anchor, rot, def.Size))
            {
                if (!c.InBounds(map)) return false;
                if (!c.Standable(map)) return false;
                if (c.GetEdifice(map) != null) return false;
            }
            return true;
        }

        
        
        
        private Comp_DoorToRooms FindRelinkEntrance()
        {
            Comp_DoorToRooms unlinked = null;
            Comp_DoorToRooms broken = null;
            Comp_DoorToRooms healthy = null;
            foreach (var map in Find.Maps)
            {
                if (map.Parent is YellowRoomsMapParent) continue; 
                foreach (var entranceDef in EntranceDefs())
                {
                    foreach (var t in map.listerThings.ThingsOfDef(entranceDef))
                    {
                        var c = t.TryGetComp<Comp_DoorToRooms>();
                        if (c == null) continue;
                        c.UpdateBrokenState();
                        if (c.IsBroken)
                        {
                            if (broken == null) broken = c;
                            continue;
                        }
                        if (!c.EnteredBefore)
                        {
                            if (unlinked == null) unlinked = c;
                            continue;
                        }
                        if (healthy == null) healthy = c;
                    }
                }
            }
            if (unlinked != null) return unlinked;
            if (broken != null) return broken;
            return healthy;
        }

        
        
        private static List<ThingDef> EntranceDefs()
        {
            var list = new List<ThingDef>(2);
            var dd = ThingDef.Named("YellowRooms_DoorToRooms"); if (dd != null) list.Add(dd);
            var pd = ThingDef.Named("YellowRooms_PortalToRooms"); if (pd != null) list.Add(pd);
            return list;
        }

        
        
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra())
                yield return g;

            if (parent == null || !parent.Spawned || !IsBroken) yield break;

            var repairDes = arsiy.Rooms.DesignationDefOf_Rooms.YellowRooms_RepairPortal;
            if (repairDes == null) yield break;

            bool marked = parent.Map.designationManager.DesignationOn(parent, repairDes) != null;
            if (marked)
            {
                yield return new Command_Action
                {
                    defaultLabel = "YellowRooms_CancelRepair".Translate(),
                    defaultDesc = "YellowRooms_CancelRepairDesc".Translate(),
                    icon = RepairIcon,
                    action = () => parent.Map?.designationManager?.TryRemoveDesignationOn(parent, repairDes)
                };
            }
            else
            {
                yield return new Command_Action
                {
                    defaultLabel = "YellowRooms_RepairPortal".Translate(),
                    defaultDesc = "YellowRooms_RepairPortalDesc".Translate(),
                    icon = RepairIcon,
                    action = () =>
                    {
                        var dm = parent.Map?.designationManager;
                        if (dm == null) return;
                        if (dm.DesignationOn(parent, repairDes) == null)
                            dm.AddDesignation(new Designation(parent, repairDes));
                    }
                };
            }
        }

        private static readonly UnityEngine.Texture2D RepairIcon =
            ContentFinder<UnityEngine.Texture2D>.Get("UI/Commands/break");

        private void TeleportToSource(Pawn pawn)
        {
            var targetMap = sourceDoor.Map;
            var landingCell = CellFinder.RandomClosewalkCellNear(sourceDoor.Position, targetMap, 2);
            LongEventHandler.QueueLongEvent(() =>
            {
                pawn.DeSpawn();
                GenSpawn.Spawn(pawn, landingCell, targetMap, Rot4.Random);
                Current.Game.CurrentMap = targetMap;
                CameraJumper.TryJumpAndSelect(pawn);
            }, "YellowRooms_Returning".Translate(), doAsynchronously: false, null);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref sourceDoor, "YellowRoomsSourceDoor");
            Scribe_Values.Look(ref needsRelink, "YellowRoomsNeedsRelink", false);
            Scribe_Values.Look(ref everLinked, "YellowRoomsEverLinked", false);
        }
    }
}
