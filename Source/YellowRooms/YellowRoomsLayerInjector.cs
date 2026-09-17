using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace arsiy.Rooms
{
    // Keeps the hidden YellowRooms ScenPart_PlanetLayerFixed present on every
    // scenario the player can start: WorldGrid.CreateRequiredLayers registers
    // one PlanetLayer per ScenPart_PlanetLayer in Find.Scenario, so a scenario
    // without the part generates no layer and the whole mod goes dormant.
    // Idempotent — scenarios that already carry the part are left untouched.
    public static class YellowRoomsLayerInjector
    {
        public const string LayerDefName = "YellowRooms";

        internal static readonly FieldInfo PartsField =
            typeof(Scenario).GetField("parts", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void EnsureInjected(Scenario scenario)
        {
            if (scenario == null || PartsField == null) return;

            var layerDef = DefDatabase<PlanetLayerDef>.GetNamedSilentFail(LayerDefName);
            var settingsDef = DefDatabase<PlanetLayerSettingsDef>.GetNamedSilentFail(LayerDefName);
            var scenPartDef = DefDatabase<ScenPartDef>.GetNamedSilentFail(LayerDefName);
            if (layerDef == null || settingsDef == null || scenPartDef == null) return;

            var parts = PartsField.GetValue(scenario) as List<ScenPart>;
            if (parts == null) return;
            if (parts.OfType<ScenPart_PlanetLayer>().Any(p => p != null && p.layer == layerDef)) return;

            parts.Add(new ScenPart_PlanetLayerFixed
            {
                def = scenPartDef,
                layer = layerDef,
                settingsDef = settingsDef,
                tag = LayerDefName,
                hide = true,
                connections = new List<LayerConnection>()
            });
        }
    }
}
