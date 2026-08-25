using HarmonyLib;
using RimWorld;
using Verse;

namespace arsiy.Rooms
{
    [HarmonyPatch(typeof(WorkGiver_RemoveRoof), "HasJobOnCell")]
    public static class Patch_RoofRemoval
    {
        public static void Postfix(Pawn pawn, IntVec3 c, bool forced, ref bool __result)
        {
            if (!__result) return;
            if (pawn?.Map == null) return;
            var roof = pawn.Map.roofGrid.RoofAt(c);
            if (roof != null && roof.defName == "YellowRoomsRoof")
                __result = false;
        }
    }
}
