using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace arsiy.Rooms
{
    
    
    
    
    
    
    
    
    
    
    
    
    public static class Patch_CampMapSize
    {
        private static readonly HashSet<string> YellowRoomsBiomes = new HashSet<string>
        {
            "YellowRooms", "YellowRooms_PoolRooms", "YellowRooms_Parking"
        };

        public static void Prefix(ref IntVec3 mapSize, MapParent parent)
        {
            if (parent == null) return;
            var biome = parent.Biome;
            if (biome == null || !YellowRoomsBiomes.Contains(biome.defName)) return;

            
            
            
            
            if (parent is Things.YellowRoomsMapParent) return;
            if (parent is RimWorld.Planet.Settlement) return;

            int size = RoomsSettings.CampMapSize;
            if (size < 25) size = 75;

            if (parent is Site site)
            {
                IntVec3 preferred = site.PreferredMapSize;
                if (preferred.x > size) size = preferred.x;
                if (preferred.z > size) size = preferred.z;
            }

            mapSize = new IntVec3(size, 1, size);
        }

        
        public static void Apply(Harmony harmony)
        {
            var target = AccessTools.Method(typeof(MapGenerator), "GenerateMap");
            if (target == null)
            {
                RoomsLog.Warning("[Rooms] MapGenerator.GenerateMap not found; camp map size setting will be ignored.");
                return;
            }
            harmony.Patch(target, prefix: new HarmonyMethod(typeof(Patch_CampMapSize), nameof(Prefix)));
        }
    }
}
