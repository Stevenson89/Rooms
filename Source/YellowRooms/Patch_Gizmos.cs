using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace arsiy.Rooms
{
    [HarmonyPatch(typeof(WorldGrid), "GetGizmos")]
    public static class Patch_WorldGizmos
    {
        private const string LayerDefName = "YellowRooms";
        private static PlanetLayerDef _yellowRoomsDef;

        private static PlanetLayerDef YellowRoomsDef
        {
            get
            {
                if (_yellowRoomsDef == null)
                    _yellowRoomsDef = DefDatabase<PlanetLayerDef>.GetNamedSilentFail(LayerDefName);
                return _yellowRoomsDef;
            }
        }

        private static PlanetLayerDef _surfaceDef;

        private static PlanetLayerDef SurfaceDef
        {
            get
            {
                if (_surfaceDef == null)
                    _surfaceDef = DefDatabase<PlanetLayerDef>.GetNamedSilentFail("Surface");
                return _surfaceDef;
            }
        }

        public static void Postfix(WorldGrid __instance, ref IEnumerable<Gizmo> __result)
        {
            var yrDef = YellowRoomsDef;
            var surfDef = SurfaceDef;
            if (yrDef == null) return;

            var selected = PlanetLayer.Selected;
            if (selected?.Def == null) return;

            var gizmos = new List<Gizmo>();
            foreach (var g in __result)
                gizmos.Add(g);

            if (selected.Def == yrDef)
            {
                gizmos.Add(new Command_Action
                {
                    defaultLabel = "YellowRooms_ReturnToPlanet".Translate(),
                    defaultDesc = "YellowRooms_ReturnToPlanetDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/YellowRooms_ViewSurface"),
                    action = () => PlanetLayer.Selected = null
                });
            }
            else if (surfDef != null && selected.Def == surfDef)
            {
                gizmos.Add(new Command_Action
                {
                    defaultLabel = "YellowRooms_ViewDimension".Translate(),
                    defaultDesc = "YellowRooms_ViewDimensionDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/YellowRooms_ViewPlanet"),
                    action = () =>
                    {
                        var layer = Find.WorldGrid.FirstLayerOfDef(yrDef);
                        if (layer != null)
                            PlanetLayer.Selected = layer;
                    }
                });
            }

            __result = gizmos;
        }
    }
}
