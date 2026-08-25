using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace arsiy.Rooms
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    public static class Patch_CampMapGenerator
    {
        
        
        private static readonly Dictionary<string, string> BiomeToGenerator = new()
        {
            { "YellowRooms", "YellowRooms" },
            { "YellowRooms_PoolRooms", "YellowRooms_PoolRooms" },
            { "YellowRooms_Parking", "YellowRooms_Parking" },
        };

        public static void Postfix(MapParent __instance, ref MapGeneratorDef __result)
        {
            
            
            
            if (__result != null && BiomeToGenerator.ContainsValue(__result.defName))
                return;

            var biome = __instance?.Biome;
            if (biome == null) return;

            if (!BiomeToGenerator.TryGetValue(biome.defName, out var genName)) return;

            var gen = DefDatabase<MapGeneratorDef>.GetNamedSilentFail(genName);
            if (gen == null)
            {
                RoomsLog.Warning($"[Rooms] MapGeneratorDef '{genName}' for biome '{biome.defName}' not found; camp will use default generator.");
                return;
            }
            __result = gen;
        }
    }
}
