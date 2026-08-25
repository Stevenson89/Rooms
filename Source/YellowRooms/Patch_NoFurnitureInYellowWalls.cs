using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace arsiy.Rooms
{
    public static class Patch_NoFurnitureInYellowWalls
    {
        private static readonly HashSet<string> WallDefNames = new HashSet<string>
        {
            "YellowRooms_Wall",
            "YellowRooms_ConcreteWall"
        };

        public static void Postfix(BuildableDef entDef, IntVec3 center, Rot4 rot, Map map,
            bool godMode, Thing thingToIgnore, ref AcceptanceReport __result)
        {
            if (!__result.Accepted || godMode || map == null) return;
            if (!(entDef is ThingDef thingDef)) return;

            foreach (IntVec3 c in GenAdj.OccupiedRect(center, rot, entDef.Size))
            {
                if (!c.InBounds(map)) continue;
                foreach (Thing thing in c.GetThingList(map))
                {
                    if (thing == thingToIgnore) continue;
                    ThingDef built = (thing.def.entityDefToBuild ?? thing.def) as ThingDef;
                    if (built != null && WallDefNames.Contains(built.defName))
                    {
                        __result = new AcceptanceReport("YellowRooms_PlaceBlockedByYellowWall".Translate());
                        return;
                    }
                }
            }
        }

        public static void Apply(Harmony harmony)
        {
            var target = AccessTools.Method(typeof(GenConstruct), nameof(GenConstruct.CanPlaceBlueprintAt));
            if (target == null)
            {
                RoomsLog.Warning("[Rooms] GenConstruct.CanPlaceBlueprintAt not found; furniture-in-wall guard disabled.");
                return;
            }
            harmony.Patch(target, postfix: new HarmonyMethod(typeof(Patch_NoFurnitureInYellowWalls), nameof(Postfix)));
        }
    }
}
