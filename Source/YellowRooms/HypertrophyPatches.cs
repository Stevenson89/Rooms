using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace arsiy.Rooms
{
    
    
    public class HediffCompProperties_HypertrophyGfx : HediffCompProperties
    {
        public HediffCompProperties_HypertrophyGfx()
        {
            compClass = typeof(HediffComp_HypertrophyGfx);
        }
    }

    public class HediffComp_HypertrophyGfx : HediffComp
    {
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            RefreshGfx();
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            RefreshGfx();
        }

        private void RefreshGfx()
        {
            if (parent?.pawn?.Drawer?.renderer != null)
            {
                parent.pawn.Drawer.renderer.SetAllGraphicsDirty();
            }
        }
    }

    
    
    
    
    
    
    [StaticConstructorOnStartup]
    public static class HypertrophyVisualPatches
    {
        private const float ScaleFactor = 2f;
        private const string HypertrophyDefName = "YellowRooms_Hypertrophy";

        private static readonly Harmony harmony = new Harmony("rimworld.arsiy.yellowrooms.hypertrophy");

        private static HediffDef hypertrophyDef;

        static HypertrophyVisualPatches()
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(PawnRenderNodeWorker), "ScaleFor"),
                postfix: new HarmonyMethod(typeof(HypertrophyVisualPatches), nameof(ScaleFor_Postfix)));

            harmony.Patch(
                original: AccessTools.Method(typeof(PawnRenderer), "BaseHeadOffsetAt"),
                postfix: new HarmonyMethod(typeof(HypertrophyVisualPatches), nameof(BaseHeadOffsetAt_Postfix)));

            harmony.Patch(
                original: AccessTools.Method(typeof(PawnRenderer), "ParallelGetPreRenderResults"),
                prefix: new HarmonyMethod(typeof(HypertrophyVisualPatches), nameof(ParallelGetPreRenderResults_Prefix)));
        }

        private static bool HasHypertrophy(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return false;
            }
            if (hypertrophyDef == null)
            {
                hypertrophyDef = DefDatabase<HediffDef>.GetNamedSilentFail(HypertrophyDefName);
            }
            return hypertrophyDef != null && pawn.health.hediffSet.HasHediff(hypertrophyDef);
        }

        
        
        
        
        
        
        
        
        private static void ScaleFor_Postfix(PawnRenderNode node, PawnDrawParms parms, ref Vector3 __result)
        {
            Pawn pawn = parms.pawn;
            if (pawn == null || !HasHypertrophy(pawn))
            {
                return;
            }
            if (node == null || node.parent == null || node.parent.parent != null)
            {
                return;
            }
            if (pawn.RaceProps.Humanlike)
            {
                if (!(node is PawnRenderNode_Body) && !(node is PawnRenderNode_Head))
                {
                    return;
                }
            }
            __result.x *= ScaleFactor;
            __result.z *= ScaleFactor;
        }

        
        
        private static void BaseHeadOffsetAt_Postfix(Pawn ___pawn, ref Vector3 __result)
        {
            if (___pawn == null || !HasHypertrophy(___pawn))
            {
                return;
            }
            __result.x *= ScaleFactor;
            __result.z *= ScaleFactor;
        }

        
        
        private static void ParallelGetPreRenderResults_Prefix(Pawn ___pawn, ref Vector3 drawLoc, ref bool disableCache)
        {
            if (___pawn == null || !HasHypertrophy(___pawn))
            {
                return;
            }
            
            disableCache = true;
            
            
            if (___pawn.GetPosture() == PawnPosture.Standing)
            {
                drawLoc.z += (ScaleFactor - 1f) / 2f;
            }
        }
    }
}
