using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace arsiy.Rooms.Things
{
    
    
    
    
    
    
    
    
    [StaticConstructorOnStartup]
    public class Comp_CeilingLamp : ThingComp
    {
        private const string BrokenDefName = "YellowRooms_CeilingLampBroken";

        
        
        private static readonly UnityEngine.Texture2D BreakIcon =
            ContentFinder<UnityEngine.Texture2D>.Get("UI/Commands/break");

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra())
                yield return g;

            if (parent == null || !parent.Spawned) yield break;

            var breakDesDef = arsiy.Rooms.DesignationDefOf_Rooms.YellowRooms_BreakLamp;
            bool alreadyMarked = parent.Map.designationManager.DesignationOn(parent, breakDesDef) != null;

            if (alreadyMarked)
            {
                
                yield return new Command_Action
                {
                    defaultLabel = "BreakLampCancel".Translate(),
                    defaultDesc = "BreakLampCancelDesc".Translate(),
                    icon = BreakIcon,
                    action = () =>
                    {
                        parent.Map.designationManager.TryRemoveDesignationOn(parent, breakDesDef);
                    },
                };
            }
            else
            {
                
                
                yield return new Command_Action
                {
                    defaultLabel = "BreakLamp".Translate(),
                    defaultDesc = "BreakLampDesc".Translate(),
                    icon = BreakIcon,
                    action = () =>
                    {
                        var dm = parent.Map.designationManager;
                        if (dm.DesignationOn(parent, breakDesDef) == null)
                            dm.AddDesignation(new Designation(parent, breakDesDef));
                    },
                };
            }
        }

        
        
        
        
        
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            foreach (var o in base.CompFloatMenuOptions(selPawn))
                yield return o;

            if (selPawn == null || !selPawn.Spawned || !selPawn.IsFreeColonist || selPawn.Map != parent.Map)
                yield break;
            if (parent == null || !parent.Spawned) yield break;

            
            if (!selPawn.CanReach(parent, PathEndMode.Touch, Danger.Deadly))
                yield break;

            var breakDesDef = arsiy.Rooms.DesignationDefOf_Rooms.YellowRooms_BreakLamp;

            yield return new FloatMenuOption("BreakLampNow".Translate(), () =>
            {
                
                
                var dm = parent.Map.designationManager;
                if (dm.DesignationOn(parent, breakDesDef) == null)
                    dm.AddDesignation(new Designation(parent, breakDesDef));
                var job = JobMaker.MakeJob(JobDefOf_Rooms.YellowRooms_BreakLamp, parent);
                selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            })
            {
                revalidateClickTarget = parent
            };
        }

        
        public void DoBreak()
        {
            if (parent == null || parent.Map == null || parent.Destroyed) return;
            var map = parent.Map;
            var cell = parent.Position;
            var rot = parent.Rotation;

            var brokenDef = ThingDef.Named(BrokenDefName);
            
            
            parent.DeSpawn();

            if (brokenDef != null)
                GenSpawn.Spawn(brokenDef, cell, map, rot, WipeMode.Vanish);

            
            var scrap = ThingDef.Named("YellowRooms_BrokenLamp");
            if (scrap != null)
                GenSpawn.Spawn(ThingMaker.MakeThing(scrap), cell, map, WipeMode.Vanish);
        }
    }
}
