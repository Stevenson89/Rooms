using System;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace arsiy.Rooms
{
    // Compatibility with "Layered Atmosphere and Orbit" (LAO).
    //
    // The Yellow Rooms layer is an isolated pocket dimension: it carries an
    // LAO mod extension with its own layer group (so LAO's scenario check
    // stays quiet) but that group deliberately belongs to no planet. LAO's
    // planet-hiding patches compare every layer's planet with the currently
    // viewed planet and force any mismatching layer invisible and
    // unclickable, which hides the whole Yellow Rooms globe.
    //
    // Giving the group a planet is not an option: LAO's TryAddPlanetLayerts
    // would then auto-add zoom connections between the surface layer and
    // Yellow Rooms, breaking the portals-only isolation. Instead we patch
    // the same vanilla getters with finalizers — Harmony runs them after
    // every prefix and postfix, LAO's included — and re-assert the correct
    // value for Yellow Rooms layers: visible and clickable exactly while
    // Yellow Rooms is selected, matching Patch_Visibility behaviour without
    // LAO.
    public static class LayeredAtmosphereOrbitCompat
    {
        private const string LAOPackageId = "MrHydralisk.LayeredAtmosphereOrbit";
        private const string LayerDefName = "YellowRooms";

        private static PlanetLayerDef yellowRoomsDef;

        // WorldDrawLayer.meshCollidersInOrder is private; vanilla Raycastable
        // only reports true when it is non-empty, and TryGetTileFromRayHit
        // dereferences it without a null check — we must keep that guard.
        private static readonly System.Reflection.FieldInfo meshCollidersField =
            AccessTools.Field(typeof(WorldDrawLayer), "meshCollidersInOrder");

        public static void PatchIfLoaded(Harmony harmony)
        {
            if (!ModsConfig.IsActive(LAOPackageId)) return;

            int patched = 0;
            patched += PatchGetter(harmony, typeof(PlanetLayer), nameof(PlanetLayer.Visible), nameof(Finalize_PlanetLayer_Visible));
            patched += PatchGetter(harmony, typeof(PlanetLayer), nameof(PlanetLayer.Raycastable), nameof(Finalize_PlanetLayer_Raycastable));
            patched += PatchGetter(harmony, typeof(WorldDrawLayer), nameof(WorldDrawLayer.Visible), nameof(Finalize_WorldDrawLayer_Visible));
            patched += PatchGetter(harmony, typeof(WorldDrawLayer), nameof(WorldDrawLayer.Raycastable), nameof(Finalize_WorldDrawLayer_Raycastable));

            // LAO's Map.FinalizeInit postfix iterates layerDef.Planet()?
            // .permamentGameConditionDefs; with no planet that foreach runs
            // over null and throws on every Yellow Rooms map.
            var laoFinalize = AccessTools.Method(AccessTools.TypeByName("LayeredAtmosphereOrbit.HarmonyPatches"), "M_FinalizeInit_Postfix");
            if (laoFinalize != null)
            {
                harmony.Patch(laoFinalize, prefix: new HarmonyMethod(typeof(LayeredAtmosphereOrbitCompat), nameof(Prefix_SkipFinalizeInitForYellowRooms)));
                patched++;
            }

            // Unconditional on purpose: one visible line confirms the compat
            // hooks registered when someone reports a LAO interaction.
            Verse.Log.Warning($"[Rooms] Layered Atmosphere and Orbit detected, {patched} compat patches applied.");
        }

        private static int PatchGetter(Harmony harmony, Type type, string propertyName, string finalizerName)
        {
            var getter = AccessTools.Property(type, propertyName)?.GetGetMethod();
            if (getter == null)
            {
                RoomsLog.Warning($"[Rooms] Could not find {type.Name}.{propertyName} getter for LAO compat.");
                return 0;
            }
            harmony.Patch(getter, finalizer: new HarmonyMethod(typeof(LayeredAtmosphereOrbitCompat), finalizerName));
            return 1;
        }

        private static bool IsYellowRooms(PlanetLayer layer)
        {
            if (layer == null) return false;
            if (yellowRoomsDef == null)
                yellowRoomsDef = DefDatabase<PlanetLayerDef>.GetNamedSilentFail(LayerDefName);
            return yellowRoomsDef != null && layer.Def == yellowRoomsDef;
        }

        private static bool YellowRoomsSelected => yellowRoomsDef != null && PlanetLayer.Selected != null
                                                   && PlanetLayer.Selected.Def == yellowRoomsDef;

        // Returning the incoming exception (or null) passes it through
        // unchanged; we only overwrite __result for Yellow Rooms layers.
        static Exception Finalize_PlanetLayer_Visible(Exception __exception, PlanetLayer __instance, ref bool __result)
        {
            if (IsYellowRooms(__instance)) __result = YellowRoomsSelected;
            return __exception;
        }

        static Exception Finalize_PlanetLayer_Raycastable(Exception __exception, PlanetLayer __instance, ref bool __result)
        {
            if (IsYellowRooms(__instance)) __result = YellowRoomsSelected;
            return __exception;
        }

        static Exception Finalize_WorldDrawLayer_Visible(Exception __exception, WorldDrawLayer __instance, ref bool __result)
        {
            if (IsYellowRooms(__instance?.planetLayer))
            {
                // Mirror the vanilla getter (base.Visible && layer selected),
                // which also preserves Rooms' isolation of the pocket dimension.
                bool baseVisible = !WorldRendererUtility.WorldBackgroundNow || __instance.VisibleInBackground;
                __result = baseVisible && PlanetLayer.Selected == __instance.planetLayer;
            }
            return __exception;
        }

        static Exception Finalize_WorldDrawLayer_Raycastable(Exception __exception, WorldDrawLayer __instance, ref bool __result)
        {
            if (IsYellowRooms(__instance?.planetLayer))
            {
                // Mirror vanilla exactly: layers without generated mesh colliders
                // (e.g. WorldDrawLayer_MouseTile) must stay non-raycastable or
                // WorldRenderer.GetTileFromRayHit throws NullReferenceException.
                var colliders = meshCollidersField?.GetValue(__instance) as System.Collections.ICollection;
                __result = colliders != null && colliders.Count > 0 && PlanetLayer.Selected == __instance.planetLayer;
            }
            return __exception;
        }

        static bool Prefix_SkipFinalizeInitForYellowRooms(Map __0)
        {
            var layerDef = __0?.Tile.LayerDef;
            return layerDef == null || layerDef.defName != "YellowRooms";
        }
    }
}
