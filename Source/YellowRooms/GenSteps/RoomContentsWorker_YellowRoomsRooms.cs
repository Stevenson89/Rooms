using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace arsiy.Rooms.GenSteps
{
    public abstract class RoomContentsWorker_YellowRoomsBase : RoomContentsWorker
    {
        protected List<Thing> SpawnBuilding(ThingDef def, LayoutRoom room, Map map, int count, Rot4? rot = null)
        {
            var result = new List<Thing>();
            if (def == null) return result;

            var stuff = def.MadeFromStuff ? ThingDefOf.Steel : null;
            for (int i = 0; i < count; i++)
            {
                if (!ComplexUtility.TryFindRandomSpawnCell(def, room, map, out var cell, 1, rot))
                    return result;
                var thing = GenSpawn.Spawn(ThingMaker.MakeThing(def, stuff), cell, map, rot ?? Rot4.North);
                if (thing != null) result.Add(thing);
            }
            return result;
        }

        protected void ScatterItem(ThingDef def, LayoutRoom room, Map map, int count, int stackCount = -1)
        {
            if (def == null || room.rects.NullOrEmpty()) return;
            for (int i = 0; i < count; i++)
            {
                var rect = room.rects.RandomElement();
                var cell = CellFinder.RandomClosewalkCellNear(rect.CenterCell, map,
                    Mathf.Min(rect.Width, rect.Height) / 2 + 1);
                if (!cell.InBounds(map) || !cell.Standable(map)) continue;

                var thing = ThingMaker.MakeThing(def);
                if (stackCount > 0) thing.stackCount = stackCount;
                GenPlace.TryPlaceThing(thing, cell, map, ThingPlaceMode.Near);
            }
        }

        protected void PlaceOnBuildings(IList<Thing> buildings, ThingDef def, Map map, int stacks, int stackCount = -1)
        {
            if (def == null || buildings == null || buildings.Count == 0) return;
            for (int i = 0; i < stacks; i++)
            {
                var building = buildings.RandomElement();
                if (building == null || !building.Spawned) continue;

                var thing = ThingMaker.MakeThing(def);
                if (stackCount > 0) thing.stackCount = stackCount;
                GenSpawn.Spawn(thing, building.Position, map);
            }
        }

        protected void SpawnStandingLamps(LayoutRoom room, Map map, int count)
        {
            SpawnBuilding(DefDatabase<ThingDef>.GetNamedSilentFail("StandingLamp"), room, map, count);
        }
    }

    public class RoomContentsWorker_YellowRoomsPortal : RoomContentsWorker_YellowRoomsBase
    {
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints = null)
        {
            base.FillRoom(map, room, faction, threatPoints);

            var portalDef = ThingDef.Named("YellowRooms_PortalFromRooms");
            if (portalDef != null
                && ComplexUtility.TryFindRandomSpawnCell(portalDef, room, map, out var cell, 1, Rot4.North))
            {
                GenSpawn.Spawn(ThingMaker.MakeThing(portalDef), cell, map, Rot4.North);
            }
            SpawnStandingLamps(room, map, Rand.RangeInclusive(1, 2));
        }
    }

    public class RoomContentsWorker_YellowRoomsSurfacePortal : RoomContentsWorker_YellowRoomsBase
    {
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints = null)
        {
            base.FillRoom(map, room, faction, threatPoints);

            var portalDef = ThingDef.Named("YellowRooms_PortalToRooms");
            if (portalDef != null
                && ComplexUtility.TryFindRandomSpawnCell(portalDef, room, map, out var cell, 1, Rot4.North))
            {
                GenSpawn.Spawn(ThingMaker.MakeThing(portalDef), cell, map, Rot4.North);
            }
            SpawnStandingLamps(room, map, Rand.RangeInclusive(1, 2));
        }
    }

    public class RoomContentsWorker_YellowRoomsCore : RoomContentsWorker_YellowRoomsBase
    {
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints = null)
        {
            base.FillRoom(map, room, faction, threatPoints);

            ScatterItem(ThingDef.Named("YellowRooms_PortalCore"), room, map, 1);
            SpawnStandingLamps(room, map, 1);
        }
    }

    public class RoomContentsWorker_YellowRoomsBedroom : RoomContentsWorker_YellowRoomsBase
    {
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints = null)
        {
            base.FillRoom(map, room, faction, threatPoints);

            var bedDef = DefDatabase<ThingDef>.GetNamedSilentFail("Bed");
            SpawnBuilding(bedDef, room, map, Mathf.Clamp(room.Area / 45, 2, 6), Rot4.East);
            SpawnStandingLamps(room, map, Rand.RangeInclusive(1, 2));
        }
    }

    public class RoomContentsWorker_YellowRoomsBarracks : RoomContentsWorker_YellowRoomsBase
    {
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints = null)
        {
            base.FillRoom(map, room, faction, threatPoints);

            var bedDef = DefDatabase<ThingDef>.GetNamedSilentFail("Bed");
            SpawnBuilding(bedDef, room, map, Mathf.Clamp(room.Area / 70, 1, 4), Rot4.East);
            SpawnStandingLamps(room, map, Rand.RangeInclusive(0, 1));
        }
    }

    public class RoomContentsWorker_YellowRoomsMess : RoomContentsWorker_YellowRoomsBase
    {
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints = null)
        {
            base.FillRoom(map, room, faction, threatPoints);

            SpawnBuilding(DefDatabase<ThingDef>.GetNamedSilentFail("Table2x2c"), room, map,
                Mathf.Clamp(room.Area / 150, 1, 3));
            SpawnBuilding(DefDatabase<ThingDef>.GetNamedSilentFail("DiningChair"), room, map,
                Mathf.Clamp(room.Area / 60, 2, 8), Rot4.North);
            ScatterItem(DefDatabase<ThingDef>.GetNamedSilentFail("MealSurvivalPackage"), room, map,
                Rand.RangeInclusive(3, 6), Rand.RangeInclusive(1, 3));
            SpawnStandingLamps(room, map, Rand.RangeInclusive(1, 2));
        }
    }

    public class RoomContentsWorker_YellowRoomsLab : RoomContentsWorker_YellowRoomsBase
    {
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints = null)
        {
            base.FillRoom(map, room, faction, threatPoints);

            var benches = SpawnBuilding(DefDatabase<ThingDef>.GetNamedSilentFail("SimpleResearchBench"), room, map,
                Mathf.Clamp(room.Area / 130, 1, 3));
            PlaceOnBuildings(benches, ThingDefOf.ComponentIndustrial, map,
                Mathf.Clamp(room.Area / 90, 1, 4), Rand.RangeInclusive(2, 6));
            SpawnStandingLamps(room, map, Rand.RangeInclusive(1, 2));
        }
    }

    public class RoomContentsWorker_YellowRoomsStorage : RoomContentsWorker_YellowRoomsBase
    {
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints = null)
        {
            base.FillRoom(map, room, faction, threatPoints);

            var shelves = SpawnBuilding(DefDatabase<ThingDef>.GetNamedSilentFail("Shelf"), room, map,
                Mathf.Clamp(room.Area / 100, 2, 8));
            PlaceOnBuildings(shelves, ThingDefOf.ComponentIndustrial, map,
                Rand.RangeInclusive(1, 2), Rand.RangeInclusive(2, 6));
            PlaceOnBuildings(shelves, ThingDefOf.Steel, map,
                Rand.RangeInclusive(1, 2), Rand.RangeInclusive(15, 40));
            PlaceOnBuildings(shelves, DefDatabase<ThingDef>.GetNamedSilentFail("MealSurvivalPackage"), map,
                Rand.RangeInclusive(1, 2), Rand.RangeInclusive(2, 4));
            SpawnStandingLamps(room, map, Rand.RangeInclusive(0, 1));
        }
    }

    public class RoomContentsWorker_YellowRoomsRustyLab : RoomContentsWorker_YellowRoomsBase
    {
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints = null)
        {
            base.FillRoom(map, room, faction, threatPoints);

            SpawnBuilding(DefDatabase<ThingDef>.GetNamedSilentFail("SimpleResearchBench"), room, map,
                Mathf.Clamp(room.Area / 130, 1, 3));
            ScatterItem(ThingDefOf.ComponentIndustrial, room, map, Mathf.Clamp(room.Area / 90, 1, 4));
            SpawnStandingLamps(room, map, Rand.RangeInclusive(1, 2));
        }
    }

    public class RoomContentsWorker_YellowRoomsRustyStorage : RoomContentsWorker_YellowRoomsBase
    {
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints = null)
        {
            base.FillRoom(map, room, faction, threatPoints);

            SpawnBuilding(DefDatabase<ThingDef>.GetNamedSilentFail("Shelf"), room, map,
                Mathf.Clamp(room.Area / 100, 2, 8));
            ScatterItem(ThingDefOf.Steel, room, map, Rand.RangeInclusive(1, 2), Rand.RangeInclusive(15, 40));
            ScatterItem(ThingDefOf.ComponentIndustrial, room, map, Rand.RangeInclusive(1, 2), Rand.RangeInclusive(2, 6));
            ScatterItem(DefDatabase<ThingDef>.GetNamedSilentFail("MealSurvivalPackage"), room, map,
                Rand.RangeInclusive(1, 2), Rand.RangeInclusive(2, 4));
            SpawnStandingLamps(room, map, Rand.RangeInclusive(0, 1));
        }
    }
}