using HarmonyLib;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace arsiy.Rooms
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        private static readonly Harmony harmony = new Harmony("rimworld.arsiy.yellowrooms");

        private static bool pendingSurvivorTraderDefenders;

        static HarmonyPatches()
        {
            YellowRoomsWhitelistApplier.Apply();
            PatchOutpostGeneration();
            PatchEdgeWallsOverLamps();
            PatchSettlementGeneration();
            PatchSettlementAttackDefenders();
            PatchFarmerCampSpawn();
            PatchQuestDrops();
            PatchGiveQuestLayerCheck();
            PatchEdgeWalkInRoofedMaps();
            PatchAbandonedBaseMapSize();
            PatchHypertrophyBodySize();
            PatchNoFace();
            PatchBiomeThreatScale();
            PatchPoolRoomsFish();
            PatchPoolRoomsRareCatchCooldown();
            PatchPassiveCloneDamage();
            PatchDetectionRaidsStillLife();
            PatchStillLifeWeaponNoDrop();
            LayeredAtmosphereOrbitCompat.PatchIfLoaded(harmony);
        }

        static void PatchStillLifeWeaponNoDrop()
        {
            var tryDropEquipment = AccessTools.Method(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.TryDropEquipment));
            if (tryDropEquipment == null)
            {
                RoomsLog.Warning("[Rooms] Could not find Pawn_EquipmentTracker.TryDropEquipment to patch; still life pawns will keep dropping their weapons.");
                return;
            }
            harmony.Patch(
                original: tryDropEquipment,
                prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Prefix_TryDropEquipment_StillLife)));
        }

        static bool Prefix_TryDropEquipment_StillLife(Pawn_EquipmentTracker __instance, ThingWithComps eq, out ThingWithComps resultingEq)
        {
            resultingEq = null;
            var pawn = __instance?.pawn;
            if (pawn == null || eq == null) return true;
            var stillLifeDef = DefDatabase<HediffDef>.GetNamedSilentFail("YellowRooms_StillLife");
            if (stillLifeDef == null || pawn.health?.hediffSet == null || !pawn.health.hediffSet.HasHediff(stillLifeDef))
                return true;
            __instance.Remove(eq);
            eq.Destroy(DestroyMode.Vanish);
            return false;
        }

        static void PatchGiveQuestLayerCheck()
        {
            var canQuestOccurOnTile = AccessTools.Method(typeof(IncidentWorker_GiveQuest), "CanQuestOccurOnTile");
            if (canQuestOccurOnTile == null)
            {
                RoomsLog.Warning("[Rooms] Could not find IncidentWorker_GiveQuest.CanQuestOccurOnTile to patch; whitelisted quests will not be given in the Yellow Rooms layer.");
                return;
            }
            harmony.Patch(
                original: canQuestOccurOnTile,
                postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Postfix_CanQuestOccurOnTile)));
        }

        static void Postfix_CanQuestOccurOnTile(PlanetTile tile, QuestScriptDef quest, ref bool __result)
        {
            if (__result || !tile.Valid || quest == null) return;
            if (quest.layerWhitelist.NullOrEmpty() || !quest.layerWhitelist.Contains(tile.LayerDef)) return;
            if (!quest.layerBlacklist.NullOrEmpty() && quest.layerBlacklist.Contains(tile.LayerDef)) return;
            __result = true;
        }

        static void PatchPassiveCloneDamage()
        {
            var postApplyDamage = AccessTools.Method(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.PostApplyDamage));
            if (postApplyDamage == null)
            {
                RoomsLog.Warning("[Rooms] Could not find Pawn_HealthTracker.PostApplyDamage to patch; passive clones will not turn hostile when damaged.");
                return;
            }
            harmony.Patch(
                original: postApplyDamage,
                postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Postfix_PawnPostApplyDamage_PassiveClone)));
        }

        private static readonly FieldInfo healthTrackerPawnField = AccessTools.Field(typeof(Pawn_HealthTracker), "pawn");

        static void Postfix_PawnPostApplyDamage_PassiveClone(Pawn_HealthTracker __instance, DamageInfo dinfo)
        {
            var pawn = healthTrackerPawnField?.GetValue(__instance) as Pawn;
            if (pawn == null || pawn.Destroyed || !pawn.Spawned || pawn.Map == null) return;
            if (pawn.Faction == null || !pawn.Faction.def.defName.StartsWith("YellowRooms_PassiveStillLife")) return;

            var comp = pawn.Map.GetComponent<arsiy.Rooms.Things.YellowRoomsMapComponent>();
            if (comp == null) return;
            comp.NotifyPassiveCloneDamaged(pawn, dinfo);
        }

        private static readonly FieldInfo waterBodyCommonFishField = AccessTools.Field(typeof(WaterBody), "commonFish");
        private static readonly FieldInfo waterBodyUncommonFishField = AccessTools.Field(typeof(WaterBody), "uncommonFish");

        static void PatchPoolRoomsFish()
        {
            if (!ModsConfig.OdysseyActive) return;
            var setFishTypes = AccessTools.Method(typeof(WaterBody), nameof(WaterBody.SetFishTypes));
            if (setFishTypes == null)
            {
                RoomsLog.Warning("[Rooms] Could not find WaterBody.SetFishTypes to patch; PoolRooms fish will only appear in pools large enough for vanilla.");
                return;
            }
            harmony.Patch(
                original: setFishTypes,
                postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Postfix_SetFishTypes_PoolRooms)));
        }

        static void Postfix_SetFishTypes_PoolRooms(WaterBody __instance)
        {
            if (!ModsConfig.OdysseyActive) return;
            if (__instance == null || __instance.map?.Biome == null || __instance.map.Biome.defName != "YellowRooms_PoolRooms") return;
            if (__instance.CommonFish.Any() || __instance.UncommonFish.Any()) return;
            var biomeFishTypes = __instance.map.Biome.fishTypes;
            if (biomeFishTypes == null) return;

            bool saltwater = __instance.waterBodyType == WaterBodyType.Saltwater;
            var common = saltwater ? biomeFishTypes.saltwater_Common : biomeFishTypes.freshwater_Common;
            var uncommon = saltwater ? biomeFishTypes.saltwater_Uncommon : biomeFishTypes.freshwater_Uncommon;
            if (common.NullOrEmpty()) return;

            var commonList = (List<ThingDef>)waterBodyCommonFishField.GetValue(__instance);
            var uncommonList = (List<ThingDef>)waterBodyUncommonFishField.GetValue(__instance);
            if (commonList == null || uncommonList == null) return;

            if (common.TryRandomElement(out var commonChance))
                commonList.Add(commonChance.fishDef);
            if (Rand.Chance(0.8f) && uncommon.TryRandomElement(out var uncommonChance))
                uncommonList.Add(uncommonChance.fishDef);
        }

        private const int PoolRoomsRareCatchCooldownTicks = 120000;

        private const float PoolRoomsRareCatchChance = 0.01f;

        static void PatchPoolRoomsRareCatchCooldown()
        {
            if (!ModsConfig.OdysseyActive) return;
            var getCatchesFor = AccessTools.Method(typeof(FishingUtility), nameof(FishingUtility.GetCatchesFor),
                new[] { typeof(Pawn), typeof(IntVec3), typeof(bool), typeof(bool).MakeByRefType() });
            if (getCatchesFor == null)
            {
                RoomsLog.Warning("[Rooms] Could not find FishingUtility.GetCatchesFor to patch; PoolRooms rare catch cooldown will stay vanilla (5 days).");
                return;
            }
            harmony.Patch(
                original: getCatchesFor,
                prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Prefix_GetCatchesFor_PoolRooms)));
        }

        static bool Prefix_GetCatchesFor_PoolRooms(Pawn pawn, IntVec3 cell, bool animalFishing, out bool rare, ref List<Thing> __result)
        {
            rare = false;
            if (!ModsConfig.OdysseyActive) return true;
            if (animalFishing) return true;
            var map = pawn?.Map;
            if (map == null || map.Biome?.defName != "YellowRooms_PoolRooms") return true;
            var tracker = map.waterBodyTracker;
            var waterBody = tracker?.WaterBodyAt(cell);
            if (waterBody == null || waterBody.waterBodyType == WaterBodyType.None) return true;

            var result = new List<Thing>();
            var fishTypes = map.Biome.fishTypes;

            if (fishTypes != null && fishTypes.rareCatchesSetMaker != null &&
                (DebugSettings.alwaysRareCatches ||
                 tracker.lastRareCatchTick == 0 ||
                 GenTicks.TicksGame - tracker.lastRareCatchTick > PoolRoomsRareCatchCooldownTicks) &&
                Rand.Chance(PoolRoomsRareCatchChance))
            {
                result.AddRange(fishTypes.rareCatchesSetMaker.root.Generate());
                if (result.Any())
                {
                    rare = true;
                    __result = result;
                    return false;
                }
            }

            ThingDef def;
            if (Rand.Chance(FishingUtility.PollutionToxfishChanceCurve.Evaluate(waterBody.PollutionPct)))
            {
                def = ThingDefOf.Fish_Toxfish;
            }
            else if ((!Rand.Chance(0.05f) || !waterBody.UncommonFish.TryRandomElement(out var uncommon)) &&
                     !waterBody.CommonFishIncludingExtras.TryRandomElement(out uncommon))
            {
                __result = result;
                return false;
            }
            else
            {
                def = uncommon;
            }

            float x = tracker.FishPopulationAt(cell);
            float num = Mathf.Min(FishingUtility.PopulationToFishYieldCurve.Evaluate(x) * pawn.GetStatValue(StatDefOf.FishingYield));
            float populationAt = tracker.FishPopulationAt(cell);
            if (num > populationAt) num = populationAt;
            int stackCount = Mathf.Max(1, Mathf.RoundToInt(num));
            var thing = ThingMaker.MakeThing(def);
            thing.stackCount = stackCount;
            result.Add(thing);
            __result = result;
            return false;
        }

        static void PatchBiomeThreatScale()
        {
            var method = AccessTools.Method(typeof(StorytellerUtility), nameof(StorytellerUtility.DefaultThreatPointsNow), new[] { typeof(IIncidentTarget) });
            if (method != null)
            {
                harmony.Patch(
                    original: method,
                    postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Postfix_DefaultThreatPointsNow)));
            }
            else
            {
                RoomsLog.Warning("[Rooms] Could not find StorytellerUtility.DefaultThreatPointsNow to patch");
            }
        }

        static void Postfix_DefaultThreatPointsNow(IIncidentTarget target, ref float __result)
        {
            if (__result <= 0f || !(target is Map map)) return;
            if (map.Biome?.defName != "YellowRooms_PoolRooms") return;
            __result *= 0.5f;
        }

        static void PatchQuestDrops()
        {

            
            
            var canPhysicallyDropInto = AccessTools.Method(typeof(DropCellFinder), nameof(DropCellFinder.CanPhysicallyDropInto));
            if (canPhysicallyDropInto != null)
            {
                harmony.Patch(
                    original: canPhysicallyDropInto,
                    postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Postfix_CanPhysicallyDropInto)));
            }

            
            var isGoodDropSpot = AccessTools.Method(typeof(DropCellFinder), nameof(DropCellFinder.IsGoodDropSpot));
            if (isGoodDropSpot != null)
            {
                harmony.Patch(
                    original: isGoodDropSpot,
                    postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Postfix_IsGoodDropSpot)));
            }

            
            var randomDropSpot = AccessTools.Method(typeof(DropCellFinder), nameof(DropCellFinder.RandomDropSpot));
            if (randomDropSpot != null)
            {
                harmony.Patch(
                    original: randomDropSpot,
                    prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Prefix_RandomDropSpot)));
            }

            
            
            var tryFindDropSpotNear = AccessTools.Method(typeof(DropCellFinder), nameof(DropCellFinder.TryFindDropSpotNear), new Type[] { typeof(IntVec3), typeof(Map), typeof(IntVec3).MakeByRefType(), typeof(bool), typeof(bool), typeof(bool), typeof(Nullable<IntVec2>), typeof(bool) });
            if (tryFindDropSpotNear != null)
            {
                harmony.Patch(
                    original: tryFindDropSpotNear,
                    prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Prefix_TryFindDropSpotNear)));
            }
            else
            {
                RoomsLog.Warning("[Rooms] Could not find TryFindDropSpotNear overload to patch");
            }

            
            var tryFindShipLandingArea = AccessTools.Method(typeof(DropCellFinder), nameof(DropCellFinder.TryFindShipLandingArea), new[] { typeof(Map), typeof(IntVec3).MakeByRefType(), typeof(Thing).MakeByRefType() });
            if (tryFindShipLandingArea != null)
            {
                harmony.Patch(
                    original: tryFindShipLandingArea,
                    prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Prefix_TryFindShipLandingArea)));
            }

            
            var tryFindSafeLandingSpot = AccessTools.Method(typeof(DropCellFinder), nameof(DropCellFinder.TryFindSafeLandingSpotCloseToColony), new[] { typeof(Map), typeof(IntVec2), typeof(Faction), typeof(int), typeof(IntVec3).MakeByRefType() });
            if (tryFindSafeLandingSpot != null)
            {
                harmony.Patch(
                    original: tryFindSafeLandingSpot,
                    prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Prefix_TryFindSafeLandingSpotCloseToColony)));
            }

            
            var makeDropPodAt = AccessTools.Method(typeof(DropPodUtility), nameof(DropPodUtility.MakeDropPodAt));
            if (makeDropPodAt != null)
            {
                harmony.Patch(
                    original: makeDropPodAt,
                    prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Prefix_MakeDropPodAt)));
            }
        }

        static void Postfix_CanPhysicallyDropInto(IntVec3 c, Map map, bool canRoofPunch, bool allowedIndoors, ref bool __result)
        {
            if (map != null && YellowRoomsUtility.IsYellowRoomsMap(map) && __result == false)
            {
                
                
                if (allowedIndoors && c.Walkable(map))
                {
                    __result = true;
                }
            }
        }

        static void Postfix_IsGoodDropSpot(IntVec3 c, Map map, bool allowFogged, bool canRoofPunch, bool allowIndoors, ref bool __result)
        {
            if (map == null || !c.InBounds(map) || !YellowRoomsUtility.IsYellowRoomsMap(map) || __result != false)
                return;

            
            if (!c.Roofed(map) || (allowIndoors && !c.Fogged(map) && c.Standable(map) && c.GetEdifice(map) == null))
            {
                
                List<Thing> thingList = c.GetThingList(map);
                bool blocked = false;
                for (int i = 0; i < thingList.Count; i++)
                {
                    Thing thing = thingList[i];
                    if (thing is IActiveTransporter || thing is Skyfaller)
                    {
                        blocked = true;
                        break;
                    }
                    if (!(thing is Building { IsClearableFreeBuilding: not false }))
                    {
                        if (thing.def.IsEdifice())
                        {
                            blocked = true;
                            break;
                        }
                        if (thing.def.preventSkyfallersLandingOn)
                        {
                            blocked = true;
                            break;
                        }
                        if (thing.def.category != ThingCategory.Plant && GenSpawn.SpawningWipes(ThingDefOf.ActiveDropPod, thing.def))
                        {
                            blocked = true;
                            break;
                        }
                    }
                }
                if (!blocked)
                    __result = true;
            }
        }


        static bool Prefix_TryFindDropSpotNear(IntVec3 center, Map map, out IntVec3 result, bool allowFogged, bool canRoofPunch, bool allowIndoors, Nullable<IntVec2> size, bool mustBeReachableFromCenter)
        {
            result = IntVec3.Invalid;
            if (map != null && YellowRoomsUtility.IsYellowRoomsMap(map))
            {
                
                if (CellFinder.TryFindRandomEdgeCellWith(cell => cell.Standable(map) && !cell.Fogged(map) && cell.GetEdifice(map) == null, map, CellFinder.EdgeRoadChance_Ignore, out var edgeCell))
                {
                    result = edgeCell;
                    return false;
                }
            }
            return true;
        }

        
        static bool Prefix_RandomDropSpot(Map map, bool standableOnly, ref IntVec3 __result)
        {
            if (map != null && YellowRoomsUtility.IsYellowRoomsMap(map))
            {
                
                if (CellFinder.TryFindRandomEdgeCellWith(cell => cell.Standable(map) && !cell.Fogged(map) && cell.GetEdifice(map) == null, map, CellFinder.EdgeRoadChance_Ignore, out var edgeCell))
                {
                    __result = edgeCell;
                    return false;
                }
            }
            return true;
        }

        
        static bool Prefix_TryFindShipLandingArea(Map map, out IntVec3 result, out Thing firstBlockingThing)
        {
            if (map != null && YellowRoomsUtility.IsYellowRoomsMap(map))
            {
                
                if (CellFinder.TryFindRandomEdgeCellWith(cell => cell.Standable(map) && !cell.Fogged(map) && cell.GetEdifice(map) == null, map, CellFinder.EdgeRoadChance_Ignore, out var edgeCell))
                {
                    result = edgeCell;
                    firstBlockingThing = null;
                    return false;
                }
            }
            result = IntVec3.Invalid;
            firstBlockingThing = null;
            return true;
        }

        
        static bool Prefix_TryFindSafeLandingSpotCloseToColony(Map map, IntVec2 size, Faction faction, int borderWidth, out IntVec3 result)
        {
            if (map != null && YellowRoomsUtility.IsYellowRoomsMap(map))
            {
                
                if (CellFinder.TryFindRandomEdgeCellWith(cell => cell.Standable(map) && !cell.Fogged(map) && cell.GetEdifice(map) == null, map, CellFinder.EdgeRoadChance_Ignore, out var edgeCell))
                {
                    result = edgeCell;
                    return false;
                }
            }
            result = IntVec3.Invalid;
            return true;
        }

        
        static void PatchHypertrophyBodySize()
        {
            harmony.Patch(
                original: AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.BodySize)),
                postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Hypertrophy_BodySize_Postfix))
            );
        }

        static void Hypertrophy_BodySize_Postfix(Pawn __instance, ref float __result)
        {
            if (__instance?.health?.hediffSet == null) return;
            var def = DefDatabase<HediffDef>.GetNamedSilentFail("YellowRooms_Hypertrophy");
            if (def != null && __instance.health.hediffSet.HasHediff(def))
                __result *= 2f;
        }

        
        
        
        
        static void PatchNoFace()
        {
            var adjustParms = AccessTools.Method(typeof(PawnRenderTree), "AdjustParms");
            if (adjustParms == null)
            {
                RoomsLog.Warning("[Rooms] Could not find PawnRenderTree.AdjustParms to patch");
                return;
            }
            harmony.Patch(
                original: adjustParms,
                postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Postfix_AdjustParms_Faceless))
            );
        }

        private static readonly System.Reflection.FieldInfo pawnField = AccessTools.Field(typeof(PawnRenderTree), "pawn");

        static void Postfix_AdjustParms_Faceless(PawnRenderTree __instance, ref PawnDrawParms parms)
        {
            Pawn pawn = (Pawn)pawnField.GetValue(__instance);
            if (pawn?.health?.hediffSet == null) return;
            var def = DefDatabase<HediffDef>.GetNamedSilentFail("YellowRooms_Faceless");
            if (def != null && pawn.health.hediffSet.HasHediff(def))
                parms.skipFlags |= RenderSkipFlagDefOf.Eyes;
        }

        
        static bool Prefix_MakeDropPodAt(IntVec3 c, Map map, ActiveTransporterInfo info, Faction faction = null)
        {
            if (map != null && YellowRoomsUtility.IsYellowRoomsMap(map) && info != null)
            {
                
                if (CellFinder.TryFindRandomEdgeCellWith(cell => cell.Standable(map) && !cell.Fogged(map) && cell.GetEdifice(map) == null, map, CellFinder.EdgeRoadChance_Ignore, out var edgeCell))
                {
                    
                    foreach (Thing thing in info.innerContainer)
                    {
                        if (thing is Pawn pawn && pawn.IsWorldPawn())
                        {
                            Find.WorldPawns.RemovePawn(pawn);
                            pawn.psychicEntropy?.SetInitialPsyfocusLevel();
                        }
                        GenPlace.TryPlaceThing(thing, edgeCell, map, ThingPlaceMode.Near);
                    }
                    info.innerContainer.Clear();
                    return false;
                }
            }
            return true;
        }

        
        static void PatchOutpostGeneration()
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(GenStep_Outpost), "Generate"),
                prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Prefix_GenStep_Outpost_Generate))
            );
        }

        static bool Prefix_GenStep_Outpost_Generate(Map map, GenStepParams parms)
        {
            if (map.Biome?.defName?.StartsWith("YellowRooms") == true
                && parms.sitePart?.def?.defName?.StartsWith("YellowRooms_") == true)
            {
                return false;
            }
            return true;
        }

        
        static void PatchEdgeWallsOverLamps()
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(SymbolResolver_EdgeWalls), "Resolve"),
                prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Prefix_EdgeWalls_Resolve))
            );
        }

        static bool Prefix_EdgeWalls_Resolve(ResolveParams rp)
        {
            var map = BaseGen.globalSettings.map;
            if (map == null || map.Biome?.defName?.StartsWith("YellowRooms") != true)
                return true;

            var wallStuff = rp.wallStuff ?? BaseGenUtility.RandomCheapWallStuff(rp.faction);
            foreach (var edgeCell in rp.rect.EdgeCells)
            {
                if (edgeCell.InBounds(map))
                    TrySpawnWallOverLamps(edgeCell, rp, wallStuff);
            }
            return false;
        }

        private static void TrySpawnWallOverLamps(IntVec3 c, ResolveParams rp, ThingDef wallStuff)
        {
            var map = BaseGen.globalSettings.map;
            var thingList = c.GetThingList(map);

            bool hasOtherNonDestroyable = false;
            for (int i = 0; i < thingList.Count; i++)
            {
                if (thingList[i] is Building_Door) return;
                if (!thingList[i].def.destroyable && !IsYellowRoomsLamp(thingList[i].def))
                    hasOtherNonDestroyable = true;
            }
            if (hasOtherNonDestroyable) return;

            for (int num = thingList.Count - 1; num >= 0; num--)
            {
                if (thingList[num].def.destroyable)
                    thingList[num].Destroy();
            }

            if (rp.chanceToSkipWallBlock.HasValue && Rand.Chance(rp.chanceToSkipWallBlock.Value))
                return;

            ThingDef wallDef = rp.wallThingDef ?? ThingDefOf.Wall;
            var wall = ThingMaker.MakeThing(wallDef, wallDef.MadeFromStuff ? wallStuff : null);
            wall.SetFaction(rp.faction);
            GenSpawn.Spawn(wall, c, map);
        }

        private static bool IsYellowRoomsLamp(ThingDef def)
        {
            return def != null
                && (def.defName == "YellowRooms_CeilingLamp" || def.defName == "YellowRooms_CeilingLampBroken");
        }

        static void PatchSettlementGeneration()
        {
            var genStep = AccessTools.Method(typeof(GenStep_Scatterer), "Generate",
                new Type[] { typeof(Map), typeof(GenStepParams) });
            if (genStep == null)
            {
                RoomsLog.Warning("[Rooms] Could not find GenStep_Scatterer.Generate to patch; settlements on YellowRooms will not generate a base.");
                return;
            }
            try
            {
                harmony.Patch(
                    original: genStep,
                    prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Prefix_GenStep_Settlement_Generate))
                );
            }
            catch (Exception e)
            {
                RoomsLog.Warning($"[Rooms] Failed to patch GenStep_Scatterer.Generate: {e.Message}");
            }
        }

        static bool Prefix_GenStep_Settlement_Generate(GenStep_Scatterer __instance, Map map, GenStepParams parms)
        {
            if (!(__instance is GenStep_Settlement))
                return true;

            if (map == null || map.Biome?.defName?.StartsWith("YellowRooms") != true)
                return true;

            if (!(map.Parent is Settlement settlement))
                return false;

            if (settlement.Faction == null || settlement.Faction == Faction.OfPlayer)
                return false;

            var genStepSettlement = (GenStep_Settlement)__instance;
            try
            {
                var center = map.Center;
                int size = Rand.RangeInclusive(34, 38);
                var rect = new CellRect(center.x - size / 2, center.z - size / 2, size, size).ClipInsideMap(map);
                RoomsLog.Message($"[Rooms] SettlementGeneration: manual settlement gen. map={map.Size}, parent={settlement.GetType().Name}, faction={settlement.Faction.def.defName}, rect={rect}");

                var zone = rect.ExpandedBy(8).ClipInsideMap(map);
                int removed = 0;
                foreach (var cell in zone.Cells)
                {
                    if (!cell.InBounds(map)) continue;
                    var edifice = cell.GetEdifice(map);
                    if (edifice != null)
                    {
                        edifice.DeSpawn(DestroyMode.Vanish);
                        removed++;
                    }
                }
                RoomsLog.Message($"[Rooms] SettlementGeneration: zone cleared. removed={removed}");

                Faction faction;
                if (pendingSurvivorTraderDefenders && settlement.Faction?.def?.defName == "YellowRooms_SurvivorTraders")
                {
                    faction = YellowRoomsUtility.GetSurvivors() ?? genStepSettlement.overrideFaction
                        ?? (map.ParentFaction != null && map.ParentFaction != Faction.OfPlayer ? map.ParentFaction : Find.FactionManager.RandomEnemyFaction());
                    pendingSurvivorTraderDefenders = false;
                    RoomsLog.Message($"[Rooms] SettlementGeneration: using hostile survivor defenders for attacked trader settlement (faction={faction?.def?.defName}).");
                }
                else
                {
                    faction = genStepSettlement.overrideFaction
                        ?? (map.ParentFaction != null && map.ParentFaction != Faction.OfPlayer
                            ? map.ParentFaction
                            : Find.FactionManager.RandomEnemyFaction());
                }

                BaseGen.globalSettings.map = map;
                BaseGen.globalSettings.minBuildings = 1 + genStepSettlement.requiredGravcoreRooms;
                BaseGen.globalSettings.minBarracks = 1;
                BaseGen.globalSettings.requiredGravcoreRooms = genStepSettlement.requiredGravcoreRooms;

                MapGenerator.SetVar("SettlementRect", rect);

                var rp = new ResolveParams
                {
                    sitePart = parms.sitePart,
                    rect = rect,
                    faction = faction,
                    settlementDontGeneratePawns = !genStepSettlement.generatePawns,
                    thingSetMakerDef = genStepSettlement.lootThingSetMaker,
                    lootMarketValue = genStepSettlement.lootMarketValue
                };
                BaseGen.symbolStack.Push("settlement", rp);
                BaseGen.Generate();
                RoomsLog.Message($"[Rooms] SettlementGeneration: done. stack={BaseGen.symbolStack.Count}, buildingsInRect={CountBuildingsInRect(map, rect)}");
                return false;
            }
            catch (Exception e)
            {
                RoomsLog.Error($"[Rooms] SettlementGeneration: manual settlement gen threw: {e}");
                return false;
            }
        }

        private static int CountBuildingsInRect(Map map, CellRect rect)
        {
            int count = 0;
            foreach (var cell in rect.Cells)
            {
                if (!cell.InBounds(map)) continue;
                if (cell.GetFirstBuilding(map) != null) count++;
            }
            return count;
        }

        static void PatchSettlementAttackDefenders()
        {
            var attackMethod = AccessTools.Method(typeof(SettlementUtility), "Attack",
                new Type[] { typeof(Caravan), typeof(Settlement) });
            if (attackMethod == null)
            {
                RoomsLog.Warning("[Rooms] Could not find SettlementUtility.Attack to patch; trader settlements will not spawn hostile defenders on attack.");
                return;
            }
            try
            {
                harmony.Patch(
                    original: attackMethod,
                    prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Prefix_SettlementUtility_Attack))
                );
            }
            catch (Exception e)
            {
                RoomsLog.Warning($"[Rooms] Failed to patch SettlementUtility.Attack: {e.Message}");
            }
        }

        static bool Prefix_SettlementUtility_Attack(Caravan caravan, Settlement settlement)
        {
            if (settlement?.Faction?.def?.defName == "YellowRooms_SurvivorTraders")
            {
                pendingSurvivorTraderDefenders = true;
                RoomsLog.Message("[Rooms] SettlementAttack: marked trader settlement for hostile survivor defender spawn.");
            }
            return true;
        }

        
        
        static void PatchFarmerCampSpawn()
        {
            
            harmony.Patch(
                original: AccessTools.Method(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), new[] { typeof(PawnGenerationRequest) }),
                postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Postfix_GeneratePawn_AddStillLife))
            );
        }

        static void Postfix_GeneratePawn_AddStillLife(Pawn __result, PawnGenerationRequest request)
        {
            if (__result == null || request.Tile.Valid == false) return;
            
            var layerDef = DefDatabase<PlanetLayerDef>.GetNamedSilentFail("YellowRooms");
            if (layerDef == null) return;
            
            var tile = Find.WorldGrid[request.Tile];
            if (tile.Layer.Def != layerDef) return;
            
            
            if (__result.Faction == null || __result.Faction == Faction.OfPlayer) return;
            
            
            
            
            if (__result.Faction.def.defName.StartsWith("YellowRooms_")) return;
            
            
            var stillLifeDef = DefDatabase<HediffDef>.GetNamedSilentFail("YellowRooms_StillLife");
            if (stillLifeDef != null && __result.health?.hediffSet != null && !__result.health.hediffSet.HasHediff(stillLifeDef))
            {
                var hediff = HediffMaker.MakeHediff(stillLifeDef, __result);
                __result.health.AddHediff(hediff);
                
                
                arsiy.Rooms.Incidents.CloneHelper.ApplyMissingParts(__result);
                arsiy.Rooms.Incidents.CloneHelper.ApplyRandomModifications(__result);
            }
        }

        static void PatchEdgeWalkInRoofedMaps()
        {
            var tryResolve = AccessTools.Method(typeof(PawnsArrivalModeWorker_EdgeWalkIn),
                nameof(PawnsArrivalModeWorker_EdgeWalkIn.TryResolveRaidSpawnCenter));
            if (tryResolve != null)
            {
                harmony.Patch(
                    original: tryResolve,
                    prefix: new HarmonyMethod(typeof(HarmonyPatches),
                        nameof(Prefix_TryResolveRaidSpawnCenter)));
            }
        }

        static bool Prefix_TryResolveRaidSpawnCenter(PawnsArrivalModeWorker_EdgeWalkIn __instance, IncidentParms parms)
        {
            if (!(parms.target is Map map) || !YellowRoomsUtility.IsYellowRoomsMap(map))
                return true;

            if (!arsiy.Rooms.Incidents.EntryCellHelper.TryFindEntryCell(map, out var cell))
                cell = CellFinder.RandomClosewalkCellNear(map.Center, map, 10);

            parms.spawnCenter = cell;
            parms.spawnRotation = Rot4.FromAngleFlat((map.Center - cell).AngleFlat);
            return false;
        }

        static void PatchAbandonedBaseMapSize()
        {
            var preferredMapSize = AccessTools.PropertyGetter(typeof(Site), nameof(Site.PreferredMapSize));
            if (preferredMapSize != null)
            {
                harmony.Patch(
                    original: preferredMapSize,
                    prefix: new HarmonyMethod(typeof(HarmonyPatches),
                        nameof(Prefix_PreferredMapSize)));
            }
        }

        static bool Prefix_PreferredMapSize(Site __instance, ref IntVec3 __result)
        {
            if (__instance is Things.AbandonedResearcherBaseSite)
            {
                __result = new IntVec3(100, 1, 100);
                return false;
            }
            return true;
        }

        static void PatchDetectionRaidsStillLife()
        {
            fTicksLeftToSendRaid = AccessTools.Field(typeof(TimedDetectionRaids), "ticksLeftToSendRaid");
            fTicksLeftTillNotifyPlayer = AccessTools.Field(typeof(TimedDetectionRaids), "ticksLeftTillNotifyPlayer");
            fRaidsSentCount = AccessTools.Field(typeof(TimedDetectionRaids), "raidsSentCount");
            if (fTicksLeftToSendRaid == null || fTicksLeftTillNotifyPlayer == null || fRaidsSentCount == null)
            {
                RoomsLog.Warning("[Rooms] Could not find TimedDetectionRaids private fields; found structure detection raids will stay vanilla (mechanoids).");
                return;
            }
            var compTick = AccessTools.Method(typeof(TimedDetectionRaids), nameof(TimedDetectionRaids.CompTickInterval));
            if (compTick == null)
            {
                RoomsLog.Warning("[Rooms] Could not find TimedDetectionRaids.CompTickInterval to patch; found structure detection raids will stay vanilla (mechanoids).");
                return;
            }
            harmony.Patch(
                original: compTick,
                prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Prefix_TimedDetectionRaids_CompTickInterval)));
        }

        static FieldInfo fTicksLeftToSendRaid;

        static FieldInfo fTicksLeftTillNotifyPlayer;

        static FieldInfo fRaidsSentCount;

        static bool Prefix_TimedDetectionRaids_CompTickInterval(TimedDetectionRaids __instance, int delta)
        {
            if (!(__instance.parent is Things.YellowRoomsFoundStructure)) return true;

            var mapParent = (MapParent)__instance.parent;
            if (!mapParent.HasMap)
            {
                __instance.ResetCountdown();
                return false;
            }

            int ticksLeftToSendRaid = (int)fTicksLeftToSendRaid.GetValue(__instance);
            int ticksLeftTillNotifyPlayer = (int)fTicksLeftTillNotifyPlayer.GetValue(__instance);
            int raidsSentCount = (int)fRaidsSentCount.GetValue(__instance);

            if (ticksLeftTillNotifyPlayer > 0)
            {
                ticksLeftTillNotifyPlayer -= delta;
                if (ticksLeftTillNotifyPlayer <= 0)
                {
                    var stillLifeFaction = YellowRoomsUtility.GetStillLifeFaction();
                    if (stillLifeFaction != null)
                    {
                        Find.LetterStack.ReceiveLetter("LetterLabelSiteCountdownStarted".Translate(),
                            "LetterTextSiteCountdownStarted".Translate(ticksLeftToSendRaid.ToStringTicksToDays(),
                                stillLifeFaction.def.pawnsPlural, stillLifeFaction), LetterDefOf.ThreatBig, mapParent);
                    }
                    __instance.alertRaidsArrivingIn = true;
                }
            }
            fTicksLeftTillNotifyPlayer.SetValue(__instance, ticksLeftTillNotifyPlayer);

            if (ticksLeftToSendRaid <= 0) return false;
            ticksLeftToSendRaid -= delta;
            if (ticksLeftToSendRaid <= 0)
            {
                var stillLifeFaction = YellowRoomsUtility.GetStillLifeFaction();
                if (stillLifeFaction != null
                    && YellowRoomsUtility.TryGetRandomHumanlikeFaction(out var raidFaction, false))
                {
                    var map = mapParent.Map;
                    float points = StorytellerUtility.DefaultThreatPointsNow(map) * 2.5f;
                    var pawns = YellowRoomsUtility.SpawnStillLifeGroupAtEdge(raidFaction, stillLifeFaction, map, points);
                    if (pawns.Count > 0)
                    {
                        var lordJob = new LordJob_AssaultColony(stillLifeFaction, canKidnap: true, canTimeoutOrFlee: true, sappers: false, useAvoidGridSmart: true);
                        LordMaker.MakeNewLord(stillLifeFaction, lordJob, map, pawns);
                        var letter = LetterMaker.MakeLetter("YellowRooms_Letter_StillLifeRaid_Title".Translate(),
                            "YellowRooms_Letter_StillLifeRaid_Text".Translate(pawns.Count),
                            LetterDefOf.ThreatBig, pawns[0]);
                        Find.LetterStack.ReceiveLetter(letter);
                        raidsSentCount++;
                    }
                }
                ticksLeftToSendRaid = (int)(__instance.delayRangeHours.RandomInRange * 2500f);
            }
            fTicksLeftToSendRaid.SetValue(__instance, ticksLeftToSendRaid);
            fRaidsSentCount.SetValue(__instance, raidsSentCount);
            return false;
        }
    }
}
