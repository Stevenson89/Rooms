using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace arsiy.Rooms
{
    
    
    
    
    
    
    
    
    
    
    
    
    public static class Patch_Visibility
    {
        private const string LayerDefName = "YellowRooms";
        private static PlanetLayerDef _yellowRoomsDef;
        private static bool _lookedUp;

        private static bool IsYellowRooms(PlanetLayer layer)
        {
            if (layer == null) return false;
            if (!_lookedUp)
            {
                _yellowRoomsDef = DefDatabase<PlanetLayerDef>.GetNamedSilentFail(LayerDefName);
                _lookedUp = true;
            }
            return _yellowRoomsDef != null && layer.Def == _yellowRoomsDef;
        }

        
        private static bool YellowRoomsSelected => _yellowRoomsDef != null && PlanetLayer.Selected != null
                                                   && PlanetLayer.Selected.Def == _yellowRoomsDef;

        
        
        
        
        
        public static void WorldSelector_SelectedLayer_Postfix(PlanetLayer value)
        {
            if (value != null && IsYellowRooms(value))
            {
                Find.World.renderer?.RegenerateAllLayersNow();
            }
        }

        
        
        
        
        
        public static void PlanetLayer_Visible_Postfix(PlanetLayer __instance, ref bool __result)
        {
            if (_yellowRoomsDef == null) return;
            if (IsYellowRooms(__instance))
            {
                if (!YellowRoomsSelected)
                    __result = false;
                return;
            }
            if (YellowRoomsSelected)
                __result = false;
        }

        
        
        
        
        
        
        public static void WorldDrawLayer_Visible_Postfix(WorldDrawLayer __instance, ref bool __result)
        {
            if (__result == false) return;
            var field = typeof(WorldDrawLayer).GetField("planetLayer", BindingFlags.Instance | BindingFlags.Public);
            if (field == null)
            {
                RoomsLog.Warning("[Rooms] Patch_Visibility: could not find WorldDrawLayer.planetLayer field — visibility patch may not work.");
                return;
            }
            var layer = (PlanetLayer)field.GetValue(__instance);
            PlanetLayer_Visible_Postfix(layer, ref __result);
        }
    }
}
