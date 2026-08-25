using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace arsiy.Rooms.Things
{
    
    
    
    
    
    
    
    
    [StaticConstructorOnStartup]
    public class Comp_DoorToRooms : Comp_YellowRoomsPortalBase
    {
private int targetTileIndex = -1;
        private bool tileAssigned;
        private bool everEntered;
        private bool broken;
        private bool exitRecreatePending;
        private bool pendingLoadCheck;

        public bool IsBroken => broken;

        
        
        public bool EnteredBefore => everEntered;

        public PlanetLayer GetYellowRoomsLayer()
        {
            var def = DefDatabase<PlanetLayerDef>.GetNamedSilentFail("YellowRooms");
            if (def == null) return null;
            return Find.WorldGrid.FirstLayerOfDef(def);
        }

        
        private void AssignRandomTile()
        {
            var layer = GetYellowRoomsLayer();
            if (layer == null || layer.TilesCount <= 0) return;

            
            
            var used = new HashSet<int>();
            if (parent?.Map != null)
            {
                foreach (var other in parent.Map.listerThings.ThingsOfDef(parent.def))
                {
                    if (other == parent) continue;
                    var c = other.TryGetComp<Comp_DoorToRooms>();
                    if (c != null && c.tileAssigned && c.targetTileIndex >= 0)
                        used.Add(c.targetTileIndex);
                }
            }

            
            for (int attempt = 0; attempt < 20; attempt++)
            {
                int candidate = Rand.Range(0, layer.TilesCount);
                if (!used.Contains(candidate))
                {
                    targetTileIndex = candidate;
                    tileAssigned = true;
                    return;
                }
            }
            targetTileIndex = Rand.Range(0, layer.TilesCount);
            tileAssigned = true;
        }

public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (respawningAfterLoad)
                pendingLoadCheck = true;
            else
                UpdateBrokenState();
        }

        public override void CompTick()
        {
            base.CompTick();
            if (pendingLoadCheck && parent != null && parent.Spawned)
            {
                pendingLoadCheck = false;
                UpdateBrokenState();
            }
        }

        public void UpdateBrokenState()
        {
            if (!everEntered || exitRecreatePending) return;
            if (LinkedExitAlive())
                broken = false;
            else
                broken = true;
        }

        
        
        
        
        
        
        
        public override string CompInspectStringExtra()
        {
            string text = base.CompInspectStringExtra();
            UpdateBrokenState();
            if (broken && parent != null && parent.Spawned)
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

        
        
        
        
        
        private bool LinkedExitAlive()
        {
            var layer = GetYellowRoomsLayer();
            if (layer == null || targetTileIndex < 0) return true;
            var tile = new PlanetTile(targetTileIndex, layer);
            var mapParent = Find.WorldObjects.AllWorldObjects
                .OfType<YellowRoomsMapParent>()
                .FirstOrDefault(mp => mp.Tile == tile);
            if (mapParent == null) return false;             
            if (mapParent.Map == null) return true;          
            foreach (var exitDef in ExitDoorDefs())
            {
                foreach (var d in mapParent.Map.listerThings.ThingsOfDef(exitDef))
                {
                    var c = d.TryGetComp<Comp_YellowRoomsReturnDoor>();
                    if (c != null && c.IsLinkedTo(parent))
                        return true;
                }
            }
            return false;
        }

        
        
        public void Notify_Repaired()
        {
            broken = false;
            exitRecreatePending = true;
        }

        
        
        
        
        
        public void Notify_Relinked(Thing returnDoor)
        {
            if (returnDoor?.Map?.Parent is YellowRoomsMapParent mp)
                targetTileIndex = (int)mp.Tile;
            tileAssigned = true;   
            broken = false;
            everEntered = true;
            exitRecreatePending = false;
        }

        public override bool CanUsePortal(out string reason)
        {
            UpdateBrokenState();
            if (broken)
            {
                reason = "YellowRooms_NeedsRepair".Translate();
                return false;
            }
            if (GetYellowRoomsLayer() == null)
            {
                reason = "YellowRooms_NoDimension".Translate();
                return false;
            }
            reason = "";
            return true;
        }

        public override string EnterLabel => "YellowRooms_CargoEnterTitle".Translate();

        public override string EnterDesc => "YellowRooms_CargoEnterDesc".Translate(parent?.def.label ?? "");

        
        
        protected override Job MakeEnterJob(Pawn pawn) =>
            JobMaker.MakeJob(JobDefOf_Rooms.EnterYellowRooms, parent);

        
        
        
        
        
        
        
        protected override void ResolveDestinationAsync()
        {
            if (parent == null || !parent.Spawned) return;
            var layer = GetYellowRoomsLayer();
            if (layer == null)
            {
                Messages.Message("YellowRooms_NoDimension".Translate(), parent, MessageTypeDefOf.RejectInput, false);
                return;
            }

            
            
            if (exitRecreatePending)
            {
                AssignRandomTile();
                exitRecreatePending = false;
            }
            if (!tileAssigned || targetTileIndex < 0) AssignRandomTile();
            if (targetTileIndex < 0)
            {
                Messages.Message("YellowRooms_NoDestination".Translate(), parent, MessageTypeDefOf.RejectInput, false);
                return;
            }

            var planetTile = new PlanetTile(targetTileIndex, layer);
            var mapParent = GetOrCreateMapParent(planetTile);
            if (mapParent == null) return;

            if (mapParent.Map != null)
            {
                AssignCargoDestination(mapParent.Map);
                return;
            }

            LongEventHandler.QueueLongEvent(() =>
            {
                var map = mapParent.GenerateYellowRoomsMap();
                if (map == null)
                {
                    Messages.Message("YellowRooms_FailedGeneration".Translate(), parent, MessageTypeDefOf.RejectInput, false);
                    YellowRoomsPortalTransfer.Cancel(this);
                    return;
                }
                AssignCargoDestination(map);
            }, "YellowRooms_Travelling".Translate(), doAsynchronously: false, null);
        }

        
        
        
        private void AssignCargoDestination(Map map)
        {
            if (map == null) return;
            var exitDoor = FindOrCreateExitDoor(map, parent, justGenerated: true);
            if (exitDoor != null)
            {
                everEntered = true;
                exitRecreatePending = false;
                cargoDestCell = exitDoor.Position;
            }
            else
            {
                cargoDestCell = CellFinder.RandomClosewalkCellNear(map.Center, map, 20);
            }
            cargoDestMap = map;

            
            try
            {
                var yrLayer = GetYellowRoomsLayer();
                if (yrLayer != null)
                {
                    Find.WorldPathGrid.RecalculateLayerPerceivedPathCosts(yrLayer);
                    Find.WorldReachability.ClearCache();
                }
            }
            catch {  }
        }

        
        
        
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra())
                yield return g;

            if (parent == null || !parent.Spawned) yield break;

            UpdateBrokenState();
            if (!broken) yield break;

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

        
        
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            foreach (var o in base.CompFloatMenuOptions(selPawn))
                yield return o;

            if (selPawn == null || !selPawn.Spawned || !selPawn.IsFreeColonist || selPawn.Map != parent.Map)
                yield break;

            yield return new FloatMenuOption("YellowRooms_Enter".Translate(), () =>
            {
                var job = JobMaker.MakeJob(JobDefOf_Rooms.EnterYellowRooms, parent);
                selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            })
            {
                revalidateClickTarget = parent
            };

            UpdateBrokenState();
            if (broken)
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

        public void EnterYellowRooms(Pawn pawn)
        {
            UpdateBrokenState();
            if (broken)
            {
                Messages.Message("YellowRooms_NeedsRepair".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            var layer = GetYellowRoomsLayer();
            if (layer == null)
            {
                Messages.Message("YellowRooms_NoDimension".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            if (!tileAssigned || targetTileIndex < 0) AssignRandomTile();

            
            
            
            
            if (exitRecreatePending)
            {
                AssignRandomTile();
                exitRecreatePending = false;
            }

            if (targetTileIndex < 0)
            {
                Messages.Message("YellowRooms_NoDestination".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            var planetTile = new PlanetTile(targetTileIndex, layer);
            var mapParent = GetOrCreateMapParent(planetTile);
            if (mapParent == null) return;

            var sourceDoor = parent;
            LongEventHandler.QueueLongEvent(() =>
            {
                Map targetMap = mapParent.Map;
                bool justGenerated = false;
                if (targetMap == null)
                {
                    targetMap = mapParent.GenerateYellowRoomsMap();
                    justGenerated = targetMap != null;
                    if (targetMap == null)
                    {
                        Messages.Message("YellowRooms_FailedGeneration".Translate(), MessageTypeDefOf.RejectInput, false);
                        return;
                    }
                }

                
                
                
                
                var exitDoor = FindOrCreateExitDoor(targetMap, sourceDoor, justGenerated);
                if (exitDoor == null)
                {
                    Messages.Message("YellowRooms_FailedWayBack".Translate(), MessageTypeDefOf.RejectInput, false);
                    return;
                }
                everEntered = true;
                exitRecreatePending = false;

                
                
                
                
                
                
                try
                {
                    var yrLayer = GetYellowRoomsLayer();
                    if (yrLayer != null)
                    {
                        Find.WorldPathGrid.RecalculateLayerPerceivedPathCosts(yrLayer);
                        Find.WorldReachability.ClearCache();
                    }
                }
                catch {  }

                
                var comp = exitDoor.TryGetComp<Comp_YellowRoomsReturnDoor>();
                var landingCell = CellFinder.RandomClosewalkCellNear(exitDoor.Position, targetMap, 2);
                pawn.DeSpawn();
                GenSpawn.Spawn(pawn, landingCell, targetMap, Rot4.Random);
                comp?.LinkToSource(sourceDoor);
                Current.Game.CurrentMap = targetMap;
                CameraJumper.TryJumpAndSelect(pawn);
            }, "YellowRooms_Travelling".Translate(), doAsynchronously: false, null);
        }

        
        
        
        
        
        
        private ThingDef ExitDoorDef()
        {
            var portalFromRooms = ThingDef.Named("YellowRooms_PortalFromRooms");
            if (parent != null && parent.def != null && parent.def.defName == "YellowRooms_PortalToRooms"
                && portalFromRooms != null)
                return portalFromRooms;
            return ThingDef.Named("YellowRooms_ReturnDoor");
        }

        
        
        
        public static bool IsExitDoor(Thing t)
        {
            if (t?.def == null) return false;
            return t.def.defName == "YellowRooms_ReturnDoor"
                || t.def.defName == "YellowRooms_PortalFromRooms";
        }

        
        
        
        
        
        
        
        private Thing FindOrCreateExitDoor(Map targetMap, Thing sourceDoor, bool justGenerated)
        {
            var exitDoorDef = ExitDoorDef();
            if (exitDoorDef == null) return null;

            
            
            
            foreach (var exitDef in ExitDoorDefs())
            {
                foreach (var d in targetMap.listerThings.ThingsOfDef(exitDef))
                {
                    var c = d.TryGetComp<Comp_YellowRoomsReturnDoor>();
                    if (c != null && c.IsLinkedTo(sourceDoor))
                        return d;
                }
            }

            
            
            SpawnExitDoorAtEdge(targetMap, sourceDoor, exitDoorDef, -1);

            
            foreach (var d in targetMap.listerThings.ThingsOfDef(exitDoorDef))
            {
                var c = d.TryGetComp<Comp_YellowRoomsReturnDoor>();
                if (c != null && c.IsLinkedTo(sourceDoor))
                    return d;
            }
            return null;
        }

        
        
        private static List<ThingDef> ExitDoorDefs()
        {
            var list = new List<ThingDef>(2);
            var rd = ThingDef.Named("YellowRooms_ReturnDoor"); if (rd != null) list.Add(rd);
            var pf = ThingDef.Named("YellowRooms_PortalFromRooms"); if (pf != null) list.Add(pf);
            return list;
        }

        
        
        
        
        
        private void SpawnExitDoorAtEdge(Map targetMap, Thing sourceDoor, ThingDef exitDoorDef, int edge)
        {
            var spawn = FindWallAdjacentSpawnNearEdge(targetMap, exitDoorDef, edge);
            IntVec3 spawnCell = spawn.cell;
            Rot4 rot = spawn.rot;
            if (!spawnCell.IsValid)
            {
                
                spawnCell = CellFinder.RandomClosewalkCellNear(targetMap.Center, targetMap, 40);
                rot = Rot4.Random;
            }
            var door = GenSpawn.Spawn(exitDoorDef, spawnCell, targetMap, rot, WipeMode.Vanish);
            door.TryGetComp<Comp_YellowRoomsReturnDoor>()?.LinkToSource(sourceDoor);
        }

        private static readonly IntVec3[] CardinalDirs =
        {
            new IntVec3(0, 0, 1),   
            new IntVec3(0, 0, -1),  
            new IntVec3(1, 0, 0),   
            new IntVec3(-1, 0, 0)   
        };

        
        
        
        
        
        
        
        
        
        
        
        
        
        private (IntVec3 cell, Rot4 rot) FindWallAdjacentSpawnNearEdge(Map targetMap, ThingDef exitDoorDef, int edge)
        {
            
            var wallDefs = new List<ThingDef>(2);
            var yw = ThingDef.Named("YellowRooms_Wall"); if (yw != null) wallDefs.Add(yw);
            var cw = ThingDef.Named("YellowRooms_ConcreteWall"); if (cw != null) wallDefs.Add(cw);
            if (wallDefs.Count == 0) return (IntVec3.Invalid, Rot4.North);

            int sx = targetMap.Size.x, sz = targetMap.Size.z;
            
            int xMin = 1, xMax = sx - 2, zMin = 1, zMax = sz - 2;
            switch (edge)
            {
                case 0: zMin = sz - sz / 3; break;              
                case 1: zMax = sz / 3; break;                   
                case 2: xMin = sx - sx / 3; break;              
                case 3: xMax = sx / 3; break;                   
                default: break;                                  
            }

            
            
            var reachable = ReachableFloor(targetMap);
            bool IsReachable(IntVec3 c) => reachable == null || reachable.Contains(c);

            IntVec3 chosen = IntVec3.Invalid;
            Rot4 chosenRot = Rot4.North;
            int found = 0;

            
            
            foreach (var wd in wallDefs)
            {
                foreach (var wall in targetMap.listerThings.ThingsOfDef(wd))
                {
                    var wp = wall.Position;
                    if (edge >= 0 && (wp.x < xMin || wp.x > xMax || wp.z < zMin || wp.z > zMax))
                        continue;
                    foreach (var delta in CardinalDirs)
                    {
                        var cell = wp + delta;
                        if (!cell.InBounds(targetMap)) continue;
                        if (!FootprintFits(cell, exitDoorDef, targetMap, IsReachable)) continue;
                        found++;
                        if (Rand.Range(0, found) == 0)
                        {
                            chosen = cell;
                            chosenRot = RotFromDelta(new IntVec3(-delta.x, 0, -delta.z));
                        }
                    }
                }
            }

            
            
            
            if (!chosen.IsValid)
            {
                for (int x = xMin; x <= xMax; x++)
                {
                    for (int z = zMin; z <= zMax; z++)
                    {
                        var cell = new IntVec3(x, 0, z);
                        if (!FootprintFits(cell, exitDoorDef, targetMap, IsReachable)) continue;
                        found++;
                        if (Rand.Range(0, found) == 0)
                        {
                            chosen = cell;
                            chosenRot = Rot4.North;
                        }
                    }
                }
            }

            return (chosen, chosenRot);
        }

        
        
        
        
        
        
        
        private static HashSet<IntVec3> ReachableFloor(Map targetMap)
        {
            try
            {
                var visited = new HashSet<IntVec3>();
                var best = new HashSet<IntVec3>();
                var deep = TerrainDefOf.WaterDeep;
                foreach (var start in targetMap.AllCells)
                {
                    if (visited.Contains(start)) continue;
                    if (!IsWalkableFloor(start, targetMap, deep)) continue;

                    
                    var region = new HashSet<IntVec3>();
                    var queue = new Queue<IntVec3>();
                    queue.Enqueue(start);
                    visited.Add(start);
                    while (queue.Count > 0)
                    {
                        var cur = queue.Dequeue();
                        region.Add(cur);
                        foreach (var d in CardinalDirs)
                        {
                            var n = cur + d;
                            if (!n.InBounds(targetMap)) continue;
                            if (visited.Contains(n)) continue;
                            if (!IsWalkableFloor(n, targetMap, deep)) continue;
                            visited.Add(n);
                            queue.Enqueue(n);
                        }
                    }
                    if (region.Count > best.Count)
                        best = region;
                }
                return best;
            }
            catch { return null; }
        }

        private static bool IsWalkableFloor(IntVec3 c, Map map, TerrainDef deep)
        {
            if (!c.InBounds(map)) return false;
            if (!c.Standable(map)) return false;
            if (deep != null && map.terrainGrid.TerrainAt(c) == deep) return false;
            var ed = c.GetEdifice(map);
            if (ed != null && ed.def.passability == Traversability.Impassable) return false;
            return true;
        }

        
        private static bool CellHasLamp(IntVec3 c, Map map)
        {
            var lamp = ThingDef.Named("YellowRooms_CeilingLamp");
            var broken = ThingDef.Named("YellowRooms_CeilingLampBroken");
            return (lamp != null && c.GetFirstThing(map, lamp) != null)
                || (broken != null && c.GetFirstThing(map, broken) != null);
        }

        
        
        private static bool CellHasExitDoor(IntVec3 c, Map map)
        {
            var rd = ThingDef.Named("YellowRooms_ReturnDoor");
            var pf = ThingDef.Named("YellowRooms_PortalFromRooms");
            return (rd != null && c.GetFirstThing(map, rd) != null)
                || (pf != null && c.GetFirstThing(map, pf) != null);
        }

        
        
        
        
        
        
        
        
        private static bool FootprintFits(IntVec3 origin, ThingDef exitDoorDef, Map map,
                                          Func<IntVec3, bool> isReachable)
        {
            int w = exitDoorDef.Size.x;
            int d = exitDoorDef.Size.z;
            for (int dx = 0; dx < w; dx++)
            {
                for (int dz = 0; dz < d; dz++)
                {
                    var c = new IntVec3(origin.x + dx, 0, origin.z + dz);
                    if (!c.InBounds(map)) return false;
                    if (!c.Standable(map)) return false;
                    if (c.GetEdifice(map) != null) return false;
                    if (CellHasLamp(c, map)) return false;
                    if (CellHasExitDoor(c, map)) return false;
                }
            }
            
            return isReachable(origin);
        }

        private static Rot4 RotFromDelta(IntVec3 d)
        {
            if (d == IntVec3.North) return Rot4.North;
            if (d == IntVec3.South) return Rot4.South;
            if (d == IntVec3.East) return Rot4.East;
            return Rot4.West;
        }

        private YellowRoomsMapParent GetOrCreateMapParent(PlanetTile planetTile)
        {
            var existing = Find.WorldObjects.AllWorldObjects
                .OfType<YellowRoomsMapParent>()
                .FirstOrDefault(mp => mp.Tile == planetTile);
            if (existing != null) return existing;

            var def = DefDatabase<WorldObjectDef>.GetNamedSilentFail("YellowRoomsSite");
            if (def == null)
            {
                RoomsLog.Error("[Rooms] YellowRoomsSite WorldObjectDef not found");
                return null;
            }
            var mapParent = (YellowRoomsMapParent)WorldObjectMaker.MakeWorldObject(def);
            mapParent.Tile = planetTile;
            mapParent.SetFaction(Faction.OfPlayer);
            Find.WorldObjects.Add(mapParent);
            return mapParent;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref targetTileIndex, "YellowRoomsTargetTile", -1);
            Scribe_Values.Look(ref tileAssigned, "YellowRoomsTileAssigned", false);
            Scribe_Values.Look(ref everEntered, "YellowRoomsEverEntered", false);
            Scribe_Values.Look(ref broken, "YellowRoomsBroken", false);
            Scribe_Values.Look(ref exitRecreatePending, "YellowRoomsExitRecreatePending", false);
        }
    }
}
