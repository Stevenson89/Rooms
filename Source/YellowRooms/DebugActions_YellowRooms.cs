using arsiy.Rooms.Incidents;
using arsiy.Rooms.Things;
using LudeonTK;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;

namespace arsiy.Rooms
{
    [StaticConstructorOnStartup]
    public static class DebugActions_YellowRooms
    {
        private const string Category = "Rooms - Tests";

        [DebugAction(category: Category, name: "Spawn still life (Drifter)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SpawnStillLifeDrifter()
        {
            var map = Find.CurrentMap;
            if (map == null) return;
            var faction = YellowRoomsUtility.GetStillLifeFaction();
            if (faction == null) { RoomsLog.Error("[Rooms] StillLife faction not found"); return; }
            float points = StorytellerUtility.DefaultThreatPointsNow(map);
            var pawns = YellowRoomsUtility.SpawnGroupAtEdge(faction, map, points, "YellowRooms_StillLife_Drifter");
            if (pawns.Count > 0)
            {
                LordMaker.MakeNewLord(faction, new LordJob_AssaultColony(faction, true, true, false, true), map, pawns);
                RoomsLog.Message($"[Rooms] Debug: spawned {pawns.Count} still life (Drifter)");
            }
        }

        [DebugAction(category: Category, name: "Spawn still life (Melee)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SpawnStillLifeMelee()
        {
            var map = Find.CurrentMap;
            if (map == null) return;
            var faction = YellowRoomsUtility.GetStillLifeFaction();
            if (faction == null) { RoomsLog.Error("[Rooms] StillLife faction not found"); return; }
            float points = StorytellerUtility.DefaultThreatPointsNow(map);
            var pawns = YellowRoomsUtility.SpawnGroupAtEdge(faction, map, points, "YellowRooms_StillLife_Melee");
            if (pawns.Count > 0)
            {
                LordMaker.MakeNewLord(faction, new LordJob_AssaultColony(faction, true, true, false, true), map, pawns);
                RoomsLog.Message($"[Rooms] Debug: spawned {pawns.Count} still life (Melee)");
            }
        }

        [DebugAction(category: Category, name: "Force researcher kidnap (nearest to downed pawn)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void DebugForceResearcherKidnap()
        {
            var map = Find.CurrentMap;
            if (map == null) return;
            bool result = YellowRoomsUtility.TryForceNearestResearcherKidnap(map);
            RoomsLog.Message(result
                ? "[Rooms] Debug: nearest researcher forced to kidnap the downed pawn"
                : "[Rooms] Debug: no eligible researcher or downed player pawn found");
        }

        [DebugAction(category: Category, name: "Spawn researcher raid", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SpawnResearcherRaid()
        {
            var map = Find.CurrentMap;
            if (map == null) return;
            var faction = YellowRoomsUtility.GetResearchers();
            if (faction == null) { RoomsLog.Error("[Rooms] Researcher faction not found"); return; }
            float points = StorytellerUtility.DefaultThreatPointsNow(map);
            var pawns = SiteMakerUtility.SpawnFactionGroup(faction, map, points, map.Center, 15);
            if (pawns.Count > 0)
            {
                AssaultHelper.StartAssault(faction, map, pawns, points);
                RoomsLog.Message($"[Rooms] Debug: spawned researcher raid ({pawns.Count} pawns)");
            }
        }

        [DebugAction(category: Category, name: "Spawn survivor raid", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SpawnSurvivorRaid()
        {
            var map = Find.CurrentMap;
            if (map == null) return;
            var faction = YellowRoomsUtility.GetSurvivors();
            if (faction == null) { RoomsLog.Error("[Rooms] Survivor faction not found"); return; }
            float points = StorytellerUtility.DefaultThreatPointsNow(map);
            var pawns = SiteMakerUtility.SpawnFactionGroup(faction, map, points, map.Center, 15);
            if (pawns.Count > 0)
            {
                AssaultHelper.StartAssault(faction, map, pawns, points);
                RoomsLog.Message($"[Rooms] Debug: spawned survivor raid ({pawns.Count} pawns)");
            }
        }

        [DebugAction(category: Category, name: "Apply StillLife hediff", allowedGameStates = AllowedGameStates.PlayingOnMap, actionType = DebugActionType.ToolMapForPawns)]
        public static void ApplyStillLife(Pawn p)
        {
            if (p == null) return;
            CloneHelper.ApplyStillLife(p);
            RoomsLog.Message($"[Rooms] Debug: applied StillLife hediff to {p.LabelShort}");
        }

        [DebugAction(category: Category, name: "Clear StillLife hediff", allowedGameStates = AllowedGameStates.PlayingOnMap, actionType = DebugActionType.ToolMapForPawns)]
        public static void ClearStillLife(Pawn p)
        {
            if (p?.health?.hediffSet == null) return;
            var def = DefDatabase<HediffDef>.GetNamedSilentFail("YellowRooms_StillLife");
            if (def == null) return;
            var h = p.health.hediffSet.GetFirstHediffOfDef(def);
            if (h != null)
            {
                p.health.RemoveHediff(h);
                RoomsLog.Message($"[Rooms] Debug: cleared StillLife hediff from {p.LabelShort}");
            }
        }

        [DebugAction(category: Category, name: "Spawn hostile clone", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SpawnHostileClone()
        {
            var map = Find.CurrentMap;
            if (map == null) return;
            var faction = YellowRoomsUtility.GetStillLifeFaction();
            if (faction == null) return;
            var original = CloneHelper.AllColonyPawns().RandomElementByWeight(_ => 1f);
            if (original == null) return;
            var clone = CloneHelper.MakeHostileClone(original, faction);
            if (clone == null) return;
            if (!EntryCellHelper.TryFindEntryCell(map, out var cell)) return;
            GenSpawn.Spawn(clone, cell, map, Rot4.Random);
            AssaultHelper.StartAssault(faction, map, new List<Pawn> { clone }, 0);
            RoomsLog.Message($"[Rooms] Debug: spawned hostile clone of {original.LabelShort}");
        }

        [DebugAction(category: Category, name: "List mushroom farm plants", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void DebugListMushroomPlants()
        {
            var map = Find.CurrentMap;
            if (map == null) return;

            var validPlants = DefDatabase<ThingDef>.AllDefs
                .Where(d => d.plant != null && d.plant.sowTags != null &&
                            d.plant.sowTags.Contains("YellowRoomsCarpet"))
                .ToList();

            RoomsLog.Message($"[Rooms] Debug: plants with YellowRoomsCarpet tag ({validPlants.Count} found):");
            foreach (var p in validPlants)
            {
                string prereqs = "";
                if (p.plant.sowResearchPrerequisites != null && p.plant.sowResearchPrerequisites.Count > 0)
                    prereqs = " [research: " + string.Join(", ", p.plant.sowResearchPrerequisites.Select(r => r.defName)) + "]";
                RoomsLog.Message($"  {p.defName} -> harvests {p.plant.harvestedThingDef?.defName ?? "none"}{prereqs}");
            }

            if (!validPlants.Any())
                RoomsLog.Warning("[Rooms] No plants found with YellowRoomsCarpet tag — DLC patches may not have applied.");

            var selected = Find.Selector.SingleSelectedThing;
            if (selected is IPlantToGrowSettable settable)
            {
                var current = settable.GetPlantDefToGrow();
                RoomsLog.Message($"[Rooms] Debug: selected farm currently set to {(current?.defName ?? "none")}");
            }
        }

        [DebugAction(category: Category, name: "Show threat timer", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void DebugShowThreatTimer()
        {
            var map = Find.CurrentMap;
            if (map == null) return;
            var comp = map.GetComponent<YellowRoomsMapComponent>();
            if (comp == null) { RoomsLog.Message("[Rooms] No YellowRoomsMapComponent on this map"); return; }

            var remainingTicks = comp.GetNextThreatTick() - Find.TickManager.TicksGame;
            if (remainingTicks < 0)
                RoomsLog.Message("[Rooms] Threat timer: READY (next check will fire)");
            else
                RoomsLog.Message($"[Rooms] Threat timer: next threat in {remainingTicks / 60000f:F1} days ({remainingTicks} ticks)");
        }

        [DebugAction(category: Category, name: "Force next threat", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void DebugForceThreat()
        {
            var map = Find.CurrentMap;
            if (map == null) return;
            var comp = map.GetComponent<YellowRoomsMapComponent>();
            if (comp == null) { RoomsLog.Message("[Rooms] No YellowRoomsMapComponent on this map"); return; }

            comp.ForceNextThreat();
            RoomsLog.Message("[Rooms] Threat forced — next MapComponentTick will try to fire.");
        }

        [DebugAction(category: Category, name: "Cycle mushroom farm plant", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void DebugCycleMushroomPlant()
        {
            var map = Find.CurrentMap;
            if (map == null) return;

            var selected = Find.Selector.SingleSelectedThing;
            if (!(selected is IPlantToGrowSettable settable))
            {
                RoomsLog.Message("[Rooms] Select a mushroom farm building to cycle its plant.");
                return;
            }

            var validPlants = DefDatabase<ThingDef>.AllDefs
                .Where(d => d.plant != null && d.plant.sowTags != null &&
                            d.plant.sowTags.Contains("YellowRoomsCarpet"))
                .ToList();

            if (validPlants.Count == 0)
            {
                RoomsLog.Warning("[Rooms] No plants with YellowRoomsCarpet tag found.");
                return;
            }

            var current = settable.GetPlantDefToGrow();
            int idx = current != null ? validPlants.IndexOf(current) : -1;
            int nextIdx = (idx + 1) % validPlants.Count;
            var next = validPlants[nextIdx];
            settable.SetPlantDefToGrow(next);
            RoomsLog.Message($"[Rooms] Debug: farm plant cycled from {(current?.defName ?? "none")} to {next.defName} ({nextIdx + 1}/{validPlants.Count})");
        }

        [DebugAction(category: "Rooms - Debug", name: "Spawn rooms", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ForceSpawnRooms()
        {
            string seed = Find.World.info.seedString;
            WorldGenStepDef[] stepDefs =
            {
                DefDatabase<WorldGenStepDef>.GetNamed("YellowRooms_Terrain"),
                DefDatabase<WorldGenStepDef>.GetNamed("YellowRooms_Factions"),
            };
            PlanetLayerSettingsDef planetLayerSettings = DefDatabase<PlanetLayerSettingsDef>.GetNamed("YellowRooms");
            PlanetLayer yellowRooms = Find.WorldGrid.RegisterPlanetLayer(DefDatabase<PlanetLayerDef>.GetNamed("YellowRooms"), planetLayerSettings.settings);
            foreach (WorldGenStepDef stepDef in stepDefs)
            {
                stepDef.worldGenStep.GenerateFresh(seed, Find.WorldGrid.FirstLayerOfDef(DefDatabase<PlanetLayerDef>.GetNamed("YellowRooms")));
            }
        }
    }
}
