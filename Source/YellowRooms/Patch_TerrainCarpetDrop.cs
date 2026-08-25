using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace arsiy.Rooms
{
    
    
    
    
    
    
    
    
    
    
    public static class Patch_TerrainCarpetDrop
    {
        private const string CarpetDefName = "CarpetYellow";
        private const string MaterialDefName = "YellowRooms_CarpetMaterial";

        
        private static readonly FieldInfo MapField =
            typeof(TerrainGrid).GetField("map", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void Prefix(TerrainGrid __instance, IntVec3 c)
        {
            if (__instance == null || MapField == null) return;
            var map = MapField.GetValue(__instance) as Map;
            if (map == null) return;
            if (!c.InBounds(map)) return;

            var top = __instance.TopTerrainAt(c);
            if (top == null || top.defName != CarpetDefName) return;

            var matDef = ThingDef.Named(MaterialDefName);
            if (matDef == null) return;

            
            
            int count = Rand.RangeInclusive(1, 2);
            var thing = ThingMaker.MakeThing(matDef);
            thing.stackCount = count;
            if (!GenPlace.TryPlaceThing(thing, c, map, ThingPlaceMode.Near, out var placed))
            {
                
                GenSpawn.Spawn(thing, c, map, WipeMode.Vanish);
            }
        }
    }
}
