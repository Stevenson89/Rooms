using HarmonyLib;
using RimWorld;
using Verse;

namespace arsiy.Rooms
{
    [HarmonyPatch(typeof(TerrainGrid), "CanRemoveTopLayerAt")]
    public static class Patch_CanRemoveTopLayerAt
    {
        public static void Postfix(TerrainGrid __instance, IntVec3 c, ref bool __result)
        {
            if (!__result) return;
            var terrain = __instance.TopTerrainAt(c);
            if (terrain != null && terrain.defName == "CeramicTile")
                __result = false;
        }
    }
}
