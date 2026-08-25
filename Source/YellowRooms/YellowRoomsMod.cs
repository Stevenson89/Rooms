using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace arsiy.Rooms
{
    
    
    
    
    
    
    
    [StaticConstructorOnStartup]
    public static class YellowRoomsMod
    {
        private const string LayerDefName = "YellowRooms";

        
        private static readonly FieldInfo PartsField =
            typeof(Scenario).GetField("parts", BindingFlags.Instance | BindingFlags.NonPublic);

        static YellowRoomsMod()
        {
            try
            {
                InjectYellowRoomsLayerIntoScenarios();
                ApplyVisibilityPatches();
                RoomsLog.Message("[Rooms] YellowRooms mod initialized (layer injected via ScenPart_PlanetLayerFixed)");
            }
            catch (Exception ex)
            {
                RoomsLog.Error($"[Rooms] Initialization failed: {ex}");
            }
        }

        private static void InjectYellowRoomsLayerIntoScenarios()
        {
            if (PartsField == null)
            {
                RoomsLog.Error("[Rooms] Could not reflect Scenario.parts field.");
                return;
            }

            var layerDef = DefDatabase<PlanetLayerDef>.GetNamedSilentFail(LayerDefName);
            var settingsDef = DefDatabase<PlanetLayerSettingsDef>.GetNamedSilentFail(LayerDefName);
            var scenPartDef = DefDatabase<ScenPartDef>.GetNamedSilentFail(LayerDefName);
            if (layerDef == null || settingsDef == null || scenPartDef == null)
            {
                RoomsLog.Error($"[Rooms] Missing def for layer injection (layer={layerDef}, settings={settingsDef}, scenPart={scenPartDef}). All three must share defName '{LayerDefName}'.");
                return;
            }

            foreach (var scenario in ScenarioLister.AllScenarios())
            {
                if (scenario == null) continue;
                var parts = PartsField.GetValue(scenario) as List<ScenPart>;
                if (parts == null) continue;

                
                if (parts.OfType<ScenPart_PlanetLayer>().Any(p => p.layer == layerDef))
                    continue;

                var part = new ScenPart_PlanetLayerFixed
                {
                    def = scenPartDef,
                    layer = layerDef,
                    settingsDef = settingsDef,
                    tag = LayerDefName,
                    hide = true,
                    connections = new List<LayerConnection>()
                };
                parts.Add(part);
            }
        }

        private static void ApplyVisibilityPatches()
        {
            var harmony = new Harmony("arsiy.rooms.YellowRooms");
            var patchType = typeof(Patch_Visibility);

            PatchIfNotNull(harmony, AccessTools.Property(typeof(PlanetLayer), "Visible")?.GetGetMethod(),
                patchType, nameof(Patch_Visibility.PlanetLayer_Visible_Postfix));
            PatchIfNotNull(harmony, AccessTools.Property(typeof(WorldDrawLayer), "Visible")?.GetGetMethod(),
                patchType, nameof(Patch_Visibility.WorldDrawLayer_Visible_Postfix));
            PatchIfNotNull(harmony, AccessTools.Property(typeof(WorldSelector), "SelectedLayer")?.GetSetMethod(),
                patchType, nameof(Patch_Visibility.WorldSelector_SelectedLayer_Postfix));
            PatchIfNotNull(harmony, AccessTools.Method(typeof(WorldGrid), "GetGizmos"),
                typeof(Patch_WorldGizmos), nameof(Patch_WorldGizmos.Postfix));
            PatchIfNotNull(harmony, AccessTools.Method(typeof(TerrainGrid), "CanRemoveTopLayerAt"),
                typeof(Patch_CanRemoveTopLayerAt), nameof(Patch_CanRemoveTopLayerAt.Postfix));
            PatchIfNotNull(harmony, AccessTools.Method(typeof(WorkGiver_RemoveRoof), "HasJobOnCell"),
                typeof(Patch_RoofRemoval), nameof(Patch_RoofRemoval.Postfix));
            
            
            
            PatchCarpetDropPrefix(harmony);

            
            Patch_RecruitBlocker.Apply(harmony);

            
            Patch_StripBlocker.Apply(harmony);

            
            PatchIfNotNull(harmony, AccessTools.Property(typeof(WITab_Terrain), "IsVisible")?.GetGetMethod(),
                typeof(Patch_TerrainTab), nameof(Patch_TerrainTab.IsVisible_Postfix));

            
            
            PatchIfNotNull(harmony, AccessTools.Property(typeof(MapParent), "MapGeneratorDef")?.GetGetMethod(),
                typeof(Patch_CampMapGenerator), nameof(Patch_CampMapGenerator.Postfix));
            
            
            
            
            PatchIfNotNull(harmony, AccessTools.Property(typeof(RimWorld.Planet.Settlement), "MapGeneratorDef")?.GetGetMethod(),
                typeof(Patch_CampMapGenerator), nameof(Patch_CampMapGenerator.Postfix));

            
            
            Patch_CampMapSize.Apply(harmony);

            Patch_NoFurnitureInYellowWalls.Apply(harmony);

            
            var settleMethod = AccessTools.Method(typeof(SettleInEmptyTileUtility), "Settle", new[] { typeof(Caravan) });
            if (settleMethod == null)
            {
                RoomsLog.Warning("[Rooms] SettleInEmptyTileUtility.Settle not found; settle in Yellow Rooms will be skipped.");
            }
            else
            {
                harmony.Patch(settleMethod,
                    prefix: new HarmonyMethod(typeof(Patch_SettleInYellowRooms), nameof(Patch_SettleInYellowRooms.Prefix)));
            }
        }
        private static void PatchIfNotNull(Harmony harmony, MethodBase method, Type patchType, string postfixName)
        {
            if (method == null)
            {
                RoomsLog.Warning($"[Rooms] Method for {postfixName} not found, skipping patch.");
                return;
            }
            harmony.Patch(method, postfix: new HarmonyMethod(patchType, postfixName));
        }

        
        
        private static void PatchCarpetDropPrefix(Harmony harmony)
        {
            var target = AccessTools.Method(typeof(TerrainGrid), "RemoveTopLayer");
            if (target == null)
            {
                RoomsLog.Warning("[Rooms] TerrainGrid.RemoveTopLayer not found; carpet will not drop when stripped.");
                return;
            }
            harmony.Patch(target,
                prefix: new HarmonyMethod(typeof(Patch_TerrainCarpetDrop), nameof(Patch_TerrainCarpetDrop.Prefix)));
        }
    }
}
