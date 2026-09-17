using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace arsiy.Rooms
{
    // The boot-time injection in YellowRoomsMod only reaches scenario objects
    // that exist at mod load. Page_SelectScenario.PreOpen calls
    // ScenarioLister.MarkDirty, and the next AllScenarios() rebuilds custom and
    // Workshop scenarios from disk into fresh objects with no injected part —
    // so starting a new game from such a scenario generated no YellowRooms
    // layer at all ("custom scenarios break the mod's worldgen layer").
    //
    // Vanilla handles the identical problem for the Orbit layer inside
    // Scenario.ExposeData (LoadingVars), appending the part to pre-Odyssey
    // scenario files on load; mirror that for YellowRooms, and add a
    // last-chance check right before layers are registered.
    public static class Patch_ScenarioLayerInjection
    {
        public static void Apply(Harmony harmony)
        {
            var exposeData = AccessTools.Method(typeof(Scenario), nameof(Scenario.ExposeData));
            if (exposeData != null)
            {
                harmony.Patch(exposeData,
                    postfix: new HarmonyMethod(typeof(Patch_ScenarioLayerInjection), nameof(ExposeData_Postfix)));
            }
            else
            {
                RoomsLog.Warning("[Rooms] Scenario.ExposeData not found; layer part will not be injected into loaded scenarios.");
            }

            var createLayers = AccessTools.Method(typeof(WorldGrid), "CreateRequiredLayers");
            if (createLayers != null)
            {
                harmony.Patch(createLayers,
                    prefix: new HarmonyMethod(typeof(Patch_ScenarioLayerInjection), nameof(CreateRequiredLayers_Prefix)));
            }
            else
            {
                RoomsLog.Warning("[Rooms] WorldGrid.CreateRequiredLayers not found; relying on ExposeData injection only.");
            }
        }

        // Fires for every scenario deserialized from disk (custom .rsc and
        // Workshop), after vanilla's own Orbit-layer block in the same stage.
        private static void ExposeData_Postfix(Scenario __instance)
        {
            if (Scribe.mode != LoadSaveMode.LoadingVars) return;
            YellowRoomsLayerInjector.EnsureInjected(__instance);
        }

        private static void CreateRequiredLayers_Prefix()
        {
            YellowRoomsLayerInjector.EnsureInjected(Find.Scenario);
        }
    }
}
