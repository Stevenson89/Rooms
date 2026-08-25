using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace arsiy.Rooms.GenSteps
{
    public class GenStep_SupplyStash : GenStep
    {
        public override int SeedPart => 18273645;

        public override void Generate(Map map, GenStepParams parms)
        {
            if (parms.sitePart?.things == null) return;

            var center = map.Center;
            var rect = new CellRect(center.x - 5, center.z - 5, 11, 11);

            CreateStashBuilding(map, rect);
            SpawnLoot(map, rect, parms.sitePart.things);
        }

        public static List<Thing> GenerateLoot(string stashType)
        {
            var loot = new List<Thing>();

            if (stashType == "Default")
            {
                loot.AddRange(MakeMixed());
                return loot;
            }

            loot.AddRange(MakeMixed());
            switch (stashType)
            {
                case "Food":
                    loot.AddRange(MakeFood());
                    break;
                case "Materials":
                    loot.AddRange(MakeMaterials());
                    break;
                case "Chemfuel":
                    loot.AddRange(MakeChemfuel());
                    break;
                case "Weapons":
                    loot.AddRange(MakeWeapons());
                    break;
            }
            return loot;
        }

        private void CreateStashBuilding(Map map, CellRect rect)
        {
            var woodStuff = ThingDefOf.WoodLog;
            var doorDef = ThingDefOf.Door;
            var wallDef = ThingDefOf.Wall;
            var center = rect.CenterCell;

            ClearStashZone(map, rect, 2);

            foreach (var cell in rect.Cells)
            {
                if (!cell.InBounds(map)) continue;

                bool isWall = cell.x == rect.minX || cell.x == rect.maxX || cell.z == rect.minZ || cell.z == rect.maxZ;
                if (isWall)
                {
                    var wall = ThingMaker.MakeThing(wallDef, woodStuff);
                    GenSpawn.Spawn(wall, cell, map, Rot4.North, WipeMode.Vanish);
                }
                else
                {
                    map.terrainGrid.SetTerrain(cell, TerrainDefOf.WoodPlankFloor);
                }
            }

            var doorCell = new IntVec3(center.x, 0, rect.maxZ);
            if (doorCell.InBounds(map))
            {
                var door = ThingMaker.MakeThing(doorDef, woodStuff);
                GenSpawn.Spawn(door, doorCell, map, Rot4.South, WipeMode.Vanish);
            }
        }

        private void ClearStashZone(Map map, CellRect rect, int extraCells)
        {
            var clearRect = new CellRect(rect.minX - extraCells, rect.minZ - extraCells,
                rect.Width + extraCells * 2, rect.Height + extraCells * 2);
            foreach (var cell in clearRect.Cells)
            {
                if (!cell.InBounds(map)) continue;
                var edifice = cell.GetEdifice(map);
                if (edifice != null)
                    edifice.DeSpawn(DestroyMode.Vanish);
            }
        }

        private void SpawnLoot(Map map, CellRect rect, ThingOwner things)
        {
            var innerRect = rect.ContractedBy(1);
            var center = innerRect.CenterCell;

            var items = things.ToArray();
            foreach (var thing in items)
            {
                if (thing.Destroyed) continue;
                var cell = CellFinder.RandomClosewalkCellNear(center, map, 4);
                GenPlace.TryPlaceThing(thing, cell, map, ThingPlaceMode.Near);
            }
        }

        private static List<Thing> MakeMixed()
        {
            var result = new List<Thing>();
            result.AddRange(MakeFood());
            result.AddRange(MakeMaterials());
            return result;
        }

        private static List<Thing> MakeFood()
        {
            var result = new List<Thing>();
            for (int i = 0; i < Rand.RangeInclusive(15, 40); i++)
            {
                var meal = ThingMaker.MakeThing(ThingDefOf.MealSurvivalPack);
                meal.stackCount = 1;
                result.Add(meal);
            }
            return result;
        }

        private static List<Thing> MakeMaterials()
        {
            var result = new List<Thing>();
            var defs = new[] { ThingDefOf.Steel, ThingDefOf.ComponentIndustrial, ThingDefOf.Plasteel };
            foreach (var def in defs)
            {
                for (int i = 0; i < Rand.RangeInclusive(2, 5); i++)
                {
                    var thing = ThingMaker.MakeThing(def);
                    thing.stackCount = def == ThingDefOf.Steel ? Rand.RangeInclusive(40, 80) : Rand.RangeInclusive(3, 8);
                    result.Add(thing);
                }
            }
            return result;
        }

        private static List<Thing> MakeChemfuel()
        {
            var result = new List<Thing>();
            for (int i = 0; i < Rand.RangeInclusive(3, 6); i++)
            {
                var chemfuel = ThingMaker.MakeThing(ThingDefOf.Chemfuel);
                chemfuel.stackCount = Rand.RangeInclusive(50, 100);
                result.Add(chemfuel);
            }
            return result;
        }

        private static List<Thing> MakeWeapons()
        {
            var result = new List<Thing>();
            var weaponDefs = DefDatabase<ThingDef>.AllDefs
                .Where(d => d.IsWeapon
                    && d.equipmentType == EquipmentType.Primary
                    && d.techLevel <= TechLevel.Industrial
                    && !d.destroyOnDrop
                    && (d.weaponTags == null || !d.weaponTags.Any(t => t == "TurretGun" || t == "Artillery")))
                .ToList();
            if (weaponDefs.Count == 0) return result;

            for (int i = 0; i < Rand.RangeInclusive(3, 8); i++)
            {
                var weapon = ThingMaker.MakeThing(weaponDefs.RandomElement());
                var quality = (QualityCategory)Rand.RangeInclusive(2, 5);
                weapon.TryGetComp<CompQuality>()?.SetQuality(quality, ArtGenerationContext.Outsider);
                result.Add(weapon);
            }
            return result;
        }
    }
}