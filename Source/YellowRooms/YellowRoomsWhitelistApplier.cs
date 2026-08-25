using System.Collections.Generic;
using RimWorld;
using Verse;

namespace arsiy.Rooms
{
    public static class YellowRoomsWhitelistApplier
    {
        public const string LayerDefName = "YellowRooms";

        private static readonly string[] DefaultVanillaIncidents =
        {
            "WandererJoin",
            "TravelerGroup",
            "VisitorGroup",
            "TraderCaravanArrival",
            "OrbitalTraderArrival",
            "BoomshroomSprout",
            "AmbrosiaSprout",
            "RaidEnemy",
            "RaidFriendly",
            "RansomDemand"
        };

        public static void Apply()
        {
            var layer = DefDatabase<PlanetLayerDef>.GetNamedSilentFail(LayerDefName);
            if (layer == null) return;
            var settings = RoomsSettings.Instance;
            if (settings == null) return;

            settings.EnsureDefaultsIfNeeded();

            ClearLayerFromAllDefs(layer);

            foreach (var def in DefDatabase<IncidentDef>.AllDefsListForReading)
            {
                if (settings.IsIncidentAllowed(def.defName))
                    AllowOnLayer(def, layer);
                else
                    BlockOnLayer(def, layer);
            }

            foreach (var def in DefDatabase<QuestScriptDef>.AllDefsListForReading)
            {
                if (settings.IsQuestAllowed(def.defName))
                    AllowOnLayer(def, layer);
                else
                    BlockOnLayer(def, layer);
            }

            foreach (var def in DefDatabase<GameConditionDef>.AllDefsListForReading)
            {
                if (settings.IsGameConditionAllowed(def.defName))
                    AllowOnLayer(def, layer);
                else
                    BlockOnLayer(def, layer);
            }

            ApplyTechnical(layer);
        }

        public static List<string> DefaultIncidents()
        {
            var result = DefNamesWithPrefix<IncidentDef>("YellowRooms_");
            foreach (var name in DefaultVanillaIncidents)
                if (!result.Contains(name))
                    result.Add(name);
            return result;
        }

        public static List<string> DefaultQuests()
        {
            return DefNamesWithPrefix<QuestScriptDef>("YellowRooms_");
        }

        public static List<string> DefaultGameConditions()
        {
            return DefNamesWithPrefix<GameConditionDef>("YellowRooms_");
        }

        private static List<string> DefNamesWithPrefix<T>(string prefix) where T : Def
        {
            var result = new List<string>();
            foreach (var def in DefDatabase<T>.AllDefsListForReading)
                if (def.defName.StartsWith(prefix))
                    result.Add(def.defName);
            return result;
        }

        private static void ClearLayerFromAllDefs(PlanetLayerDef layer)
        {
            foreach (var def in DefDatabase<IncidentDef>.AllDefsListForReading)
            {
                def.layerWhitelist?.Remove(layer);
                def.layerBlacklist?.Remove(layer);
            }
            foreach (var def in DefDatabase<QuestScriptDef>.AllDefsListForReading)
            {
                def.layerWhitelist?.Remove(layer);
                def.layerBlacklist?.Remove(layer);
            }
            foreach (var def in DefDatabase<GameConditionDef>.AllDefsListForReading)
            {
                def.layerWhitelist?.Remove(layer);
                def.layerBlacklist?.Remove(layer);
            }
        }

        private static void AllowOnLayer(IncidentDef def, PlanetLayerDef layer)
        {
            def.layerWhitelist ??= new List<PlanetLayerDef>();
            if (!def.layerWhitelist.Contains(layer))
                def.layerWhitelist.Add(layer);
            def.layerBlacklist?.Remove(layer);
        }

        private static void AllowOnLayer(QuestScriptDef def, PlanetLayerDef layer)
        {
            def.layerWhitelist ??= new List<PlanetLayerDef>();
            if (!def.layerWhitelist.Contains(layer))
                def.layerWhitelist.Add(layer);
            def.layerBlacklist?.Remove(layer);
        }

        private static void AllowOnLayer(GameConditionDef def, PlanetLayerDef layer)
        {
            def.layerWhitelist ??= new List<PlanetLayerDef>();
            if (!def.layerWhitelist.Contains(layer))
                def.layerWhitelist.Add(layer);
            def.layerBlacklist?.Remove(layer);
        }

        private static void BlockOnLayer(IncidentDef def, PlanetLayerDef layer)
        {
            def.layerBlacklist ??= new List<PlanetLayerDef>();
            if (!def.layerBlacklist.Contains(layer))
                def.layerBlacklist.Add(layer);
            def.layerWhitelist?.Remove(layer);
        }

        private static void BlockOnLayer(QuestScriptDef def, PlanetLayerDef layer)
        {
            def.layerBlacklist ??= new List<PlanetLayerDef>();
            if (!def.layerBlacklist.Contains(layer))
                def.layerBlacklist.Add(layer);
            def.layerWhitelist?.Remove(layer);
        }

        private static void BlockOnLayer(GameConditionDef def, PlanetLayerDef layer)
        {
            def.layerBlacklist ??= new List<PlanetLayerDef>();
            if (!def.layerBlacklist.Contains(layer))
                def.layerBlacklist.Add(layer);
            def.layerWhitelist?.Remove(layer);
        }

        private static void ApplyTechnical(PlanetLayerDef layer)
        {
            foreach (var biomeDef in DefDatabase<BiomeDef>.AllDefsListForReading)
            {
                if (!biomeDef.defName.StartsWith("YellowRooms")) continue;
                biomeDef.layerWhitelist ??= new List<PlanetLayerDef>();
                if (!biomeDef.layerWhitelist.Contains(layer))
                    biomeDef.layerWhitelist.Add(layer);
            }

            foreach (var factionDef in DefDatabase<FactionDef>.AllDefsListForReading)
            {
                if (!factionDef.defName.StartsWith("YellowRooms")) continue;
                factionDef.layerWhitelist ??= new List<PlanetLayerDef>();
                if (!factionDef.layerWhitelist.Contains(layer))
                    factionDef.layerWhitelist.Add(layer);
                factionDef.arrivalLayerWhitelist ??= new List<PlanetLayerDef>();
                if (!factionDef.arrivalLayerWhitelist.Contains(layer))
                    factionDef.arrivalLayerWhitelist.Add(layer);
            }

            foreach (var arrivalModeDef in DefDatabase<PawnsArrivalModeDef>.AllDefsListForReading)
            {
                arrivalModeDef.layerWhitelist ??= new List<PlanetLayerDef>();
                if (!arrivalModeDef.layerWhitelist.Contains(layer))
                    arrivalModeDef.layerWhitelist.Add(layer);
            }

            foreach (var factionDef in DefDatabase<FactionDef>.AllDefsListForReading)
            {
                if (factionDef.defName.StartsWith("YellowRooms")) continue;
                if (factionDef.defName == "Salvagers") continue;
                factionDef.arrivalLayerWhitelist ??= new List<PlanetLayerDef>();
                if (!factionDef.arrivalLayerWhitelist.Contains(layer))
                    factionDef.arrivalLayerWhitelist.Add(layer);
            }
        }
    }
}