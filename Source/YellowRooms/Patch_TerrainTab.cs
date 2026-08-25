using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace arsiy.Rooms
{
    [StaticConstructorOnStartup]
    public static class Patch_TerrainTab
    {
        private const string LayerDefName = "YellowRooms";

        public static void IsVisible_Postfix(WITab_Terrain __instance, ref bool __result)
        {
            if (__result) return;

            var prop = AccessTools.Property(typeof(WITab), "SelPlanetTile");
            if (prop == null) return;

            var tile = (PlanetTile)prop.GetValue(__instance);
            if (!tile.Valid) return;
            if (tile.Layer?.Def?.defName == LayerDefName)
                __result = true;
        }
    }
}
