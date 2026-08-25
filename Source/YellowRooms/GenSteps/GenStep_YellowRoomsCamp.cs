using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.BaseGen;
using Verse;

namespace arsiy.Rooms.GenSteps
{
    public class GenStep_YellowRoomsCamp : GenStep
    {
        public override int SeedPart => 138692457;

        public override void Generate(Map map, GenStepParams parms)
        {
            try
            {
                var center = map.Center;
                if (!center.InBounds(map))
                {
                    RoomsLog.Message("[Rooms] YellowRoomsCamp: map center out of bounds, aborting.");
                    return;
                }

                var outpostRect = new CellRect(center.x - 8, center.z - 8, 16, 16);
                var zoneRect = outpostRect.ExpandedBy(6);

                var sitePartInfo = parms.sitePart != null
                    ? $"{parms.sitePart.def.defName} (threatPoints={parms.sitePart.parms.threatPoints}, expectedEnemyCount={parms.sitePart.expectedEnemyCount})"
                    : "null";
                RoomsLog.Message($"[Rooms] YellowRoomsCamp: running. center={center}, outpostRect={outpostRect}, zone={zoneRect}, sitePart={sitePartInfo}, mapSize={map.Size}");

                int removed = ClearZone(map, zoneRect);
                int lampsLeft = CountLamps(map, zoneRect);
                int edificesLeft = CountEdifices(map, zoneRect);
                RoomsLog.Message($"[Rooms] YellowRoomsCamp: zone cleared. removed={removed}, lampsLeft={lampsLeft}, edificesLeft={edificesLeft}");

                MapGenerator.SetVar("RectOfInterest", outpostRect);
                var outpost = new GenStep_Outpost { forcedRect = outpostRect };

                int buildingsBefore = CountBuildings(map, outpostRect);
                RoomsLog.Message($"[Rooms] YellowRoomsCamp: before GenStep_Outpost. stack={RimWorld.BaseGen.BaseGen.symbolStack.Count}, mapSet={RimWorld.BaseGen.BaseGen.globalSettings.map == map}, buildingsInRect={buildingsBefore}");
                try
                {
                    outpost.Generate(map, parms);
                }
                catch (Exception e)
                {
                    RoomsLog.Error($"[Rooms] YellowRoomsCamp: GenStep_Outpost.Generate threw: {e}");
                }
                int buildingsAfter = CountBuildings(map, outpostRect);
                RoomsLog.Message($"[Rooms] YellowRoomsCamp: after GenStep_Outpost. stack={RimWorld.BaseGen.BaseGen.symbolStack.Count}, buildingsInRect={buildingsAfter}, thingsInRect={CountThings(map, outpostRect)}");

                if (buildingsAfter <= buildingsBefore && CountPawns(map, outpostRect.ExpandedBy(4)) <= 0)
                {
                    RoomsLog.Message($"[Rooms] YellowRoomsCamp: outpost empty (no new buildings), pushing settlement symbol manually.");
                    TryBuildSettlementDirectly(map, parms, outpostRect);
                }
            }
            catch (Exception e)
            {
                RoomsLog.Error($"[Rooms] YellowRoomsCamp: genstep threw: {e}");
            }
        }

        private static void TryBuildSettlementDirectly(Map map, GenStepParams parms, CellRect outpostRect)
        {
            try
            {
                var faction = YellowRoomsUtility.GetSurvivors()
                    ?? (map.ParentFaction != null && map.ParentFaction != Faction.OfPlayer
                        ? map.ParentFaction
                        : Find.FactionManager.RandomEnemyFaction());
                RoomsLog.Message($"[Rooms] YellowRoomsCamp: manual settlement. faction={faction?.def.defName}");

                RimWorld.BaseGen.BaseGen.globalSettings.map = map;
                RimWorld.BaseGen.BaseGen.globalSettings.minBuildings = 1;
                RimWorld.BaseGen.BaseGen.globalSettings.minBarracks = 1;
                RimWorld.BaseGen.BaseGen.globalSettings.requiredWorshippedTerminalRooms = 0;
                RimWorld.BaseGen.BaseGen.globalSettings.requiredGravcoreRooms = 0;
                RimWorld.BaseGen.BaseGen.globalSettings.maxFarms = 0;

                var rp = new ResolveParams
                {
                    rect = outpostRect,
                    faction = faction,
                    edgeDefenseWidth = 2,
                    edgeDefenseTurretsCount = Rand.RangeInclusive(0, 1),
                    edgeDefenseMortarsCount = 0,
                    pawnGroupKindDef = PawnGroupKindDefOf.Combat,
                    settlementPawnGroupPoints = parms.sitePart != null ? parms.sitePart.parms.threatPoints : 1000f,
                    settlementPawnGroupSeed = parms.sitePart != null ? OutpostSitePartUtility.GetPawnGroupMakerSeed(parms.sitePart.parms) : (int?)null,
                    lootMarketValue = parms.sitePart != null ? parms.sitePart.parms.lootMarketValue : 1800f
                };
                if (parms.sitePart != null)
                {
                    rp.sitePart = parms.sitePart;
                    rp.bedCount = parms.sitePart.expectedEnemyCount == -1 ? (int?)null : parms.sitePart.expectedEnemyCount;
                }

                RimWorld.BaseGen.BaseGen.symbolStack.Push("settlement", rp);
                RoomsLog.Message($"[Rooms] YellowRoomsCamp: manual settlement pushed, stack={RimWorld.BaseGen.BaseGen.symbolStack.Count}");
                try
                {
                    RimWorld.BaseGen.BaseGen.Generate();
                }
                catch (Exception e)
                {
                    RoomsLog.Error($"[Rooms] YellowRoomsCamp: manual BaseGen.Generate threw: {e}");
                }
                RoomsLog.Message($"[Rooms] YellowRoomsCamp: after manual Generate. stack={RimWorld.BaseGen.BaseGen.symbolStack.Count}, buildingsInRect={CountBuildings(map, outpostRect)}, thingsInRect={CountThings(map, outpostRect)}");
            }
            catch (Exception e)
            {
                RoomsLog.Error($"[Rooms] YellowRoomsCamp: manual settlement threw: {e}");
            }
        }

        private static int ClearZone(Map map, CellRect rect)
        {
            int removed = 0;
            foreach (var cell in rect.Cells)
            {
                if (!cell.InBounds(map)) continue;
                var edifice = cell.GetEdifice(map);
                if (edifice != null)
                {
                    edifice.DeSpawn(DestroyMode.Vanish);
                    removed++;
                }
            }
            return removed;
        }

        private static int CountLamps(Map map, CellRect rect)
        {
            var lamp = ThingDef.Named("YellowRooms_CeilingLamp");
            var broken = ThingDef.Named("YellowRooms_CeilingLampBroken");
            int count = 0;
            foreach (var cell in rect.Cells)
            {
                if (!cell.InBounds(map)) continue;
                if (lamp != null && cell.GetFirstThing(map, lamp) != null) count++;
                if (broken != null && cell.GetFirstThing(map, broken) != null) count++;
            }
            return count;
        }

        private static int CountEdifices(Map map, CellRect rect)
        {
            int count = 0;
            foreach (var cell in rect.Cells)
            {
                if (!cell.InBounds(map)) continue;
                if (cell.GetEdifice(map) != null) count++;
            }
            return count;
        }

        private static int CountBuildings(Map map, CellRect rect)
        {
            int count = 0;
            foreach (var cell in rect.Cells)
            {
                if (!cell.InBounds(map)) continue;
                if (cell.GetFirstBuilding(map) != null) count++;
            }
            return count;
        }

        private static int CountThings(Map map, CellRect rect)
        {
            var seen = new HashSet<Thing>();
            foreach (var cell in rect.Cells)
            {
                if (!cell.InBounds(map)) continue;
                foreach (var thing in map.thingGrid.ThingsListAt(cell))
                    seen.Add(thing);
            }
            return seen.Count;
        }

        private static int CountPawns(Map map, CellRect rect)
        {
            int count = 0;
            foreach (var pawn in map.mapPawns.AllPawns)
            {
                if (rect.Contains(pawn.Position)) count++;
            }
            return count;
        }
    }
}
