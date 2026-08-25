using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace arsiy.Rooms.GenSteps
{
    public class GenStep_AbandonedResearcherBase : GenStep
    {
        private const string LayoutDefName = "YellowRooms_AbandonedResearcherBase";
        private const int BaseWidth = 30;
        private const int BaseHeight = 30;
        private const int ClearMargin = 6;
        private Map _map;

        public override int SeedPart => 28473912;

        public override void Generate(Map map, GenStepParams parms)
        {
            _map = map;
            var researcherFaction = YellowRoomsUtility.GetResearchers();
            var stillLifeFaction = YellowRoomsUtility.GetStillLifeFaction();

            if (researcherFaction == null)
            {
                RoomsLog.Warning("[Rooms] AbandonedResearcherBase: Researcher faction not found");
                return;
            }

            var layoutDef = DefDatabase<StructureLayoutDef>.GetNamedSilentFail(LayoutDefName);
            if (layoutDef == null)
            {
                RoomsLog.Warning($"[Rooms] AbandonedResearcherBase: layout def {LayoutDefName} not found");
                return;
            }

            var center = map.Center;
            var size = new IntVec2(BaseWidth, BaseHeight);
            var pos = new IntVec3(center.x - size.x / 2, 0, center.z - size.z / 2);

            var clearRect = new CellRect(pos.x - ClearMargin, pos.z - ClearMargin,
                size.x + ClearMargin * 2, size.z + ClearMargin * 2);
            clearRect.ClipInsideMap(map);
            ClearWalls(clearRect);

            var layoutParms = new StructureGenParams { size = size, faction = researcherFaction };
            var sketch = layoutDef.Worker.GenerateStructureSketch(layoutParms);
            layoutDef.Worker.Spawn(sketch, map, pos, null, new List<Thing>(), true, false, researcherFaction);

            var buildingRect = sketch.structureLayout.container;

            SpawnResearcherCorpses(buildingRect, researcherFaction);
            SpawnHostileStillLife(center, stillLifeFaction);
            ScatterMetalDebris(buildingRect);
            SpawnBloodFilth(buildingRect);
        }

        private void ClearWalls(CellRect rect)
        {
            var yellowWallDef = ThingDef.Named("YellowRooms_Wall");
            var concreteWallDef = ThingDef.Named("YellowRooms_ConcreteWall");
            var clearFloor = TerrainDefOf.Concrete;

            foreach (var cell in rect.Cells)
            {
                if (!cell.InBounds(_map)) continue;

                var edifice = cell.GetEdifice(_map);
                if (edifice != null)
                {
                    if ((yellowWallDef != null && edifice.def == yellowWallDef) ||
                        (concreteWallDef != null && edifice.def == concreteWallDef))
                    {
                        edifice.DeSpawn();
                    }
                    else
                    {
                        edifice.Destroy();
                    }
                }

                _map.terrainGrid.SetTerrain(cell, clearFloor);
            }
        }

        private void SpawnResearcherCorpses(CellRect rect, Faction researcherFaction)
        {
            var suitDef = ThingDef.Named("YellowRooms_HazmatSuit");
            var helmetDef = ThingDef.Named("YellowRooms_HazmatHelmet");
            var colonistKind = PawnKindDef.Named("Colonist");

            if (suitDef == null || helmetDef == null || colonistKind == null)
            {
                RoomsLog.Warning("[Rooms] AbandonedResearcherBase: Missing hazmat suit/helmet/colonist kind defs");
                return;
            }

            var clothDef = ThingDefOf.Cloth;

            int corpseCount = Rand.RangeInclusive(3, 6);
            for (int i = 0; i < corpseCount; i++)
            {
                var req = new PawnGenerationRequest(colonistKind, researcherFaction,
                    PawnGenerationContext.NonPlayer,
                    tile: _map.Tile, forceGenerateNewPawn: true, allowDowned: false,
                    colonistRelationChanceFactor: 0f);
                var pawn = PawnGenerator.GeneratePawn(req);

                if (suitDef != null)
                    pawn.apparel?.Wear((Apparel)ThingMaker.MakeThing(suitDef, clothDef));
                if (helmetDef != null)
                    pawn.apparel?.Wear((Apparel)ThingMaker.MakeThing(helmetDef, clothDef));

                pawn.Kill(null);
                if (pawn.Corpse != null)
                {
                    var cell = CellFinder.RandomClosewalkCellNear(rect.CenterCell, _map, rect.Width / 2 + 3);
                    GenPlace.TryPlaceThing(pawn.Corpse, cell, _map, ThingPlaceMode.Near);
                }
            }
        }

        private void SpawnHostileStillLife(IntVec3 center, Faction stillLifeFaction)
        {
            if (stillLifeFaction == null) return;

            float points = StorytellerUtility.DefaultThreatPointsNow(_map) * 0.5f;
            int count = Mathf.Max(7, YellowRoomsUtility.StillLifeCountFor(points));

            var kinds = new[]
            {
                PawnKindDef.Named("YellowRooms_StillLife_Drifter"),
                PawnKindDef.Named("YellowRooms_StillLife_Melee")
            }.Where(k => k != null).ToList();

            if (kinds.Count == 0) return;

            for (int i = 0; i < count; i++)
            {
                var kind = kinds.RandomElement();
                var cell = CellFinder.RandomClosewalkCellNear(center, _map, 20);
                var pawn = YellowRoomsUtility.SpawnHostilePawn(kind, stillLifeFaction, cell, _map);
                if (pawn != null)
                {
                    var lordJob = new LordJob_DefendPoint(center, 25f);
                    LordMaker.MakeNewLord(stillLifeFaction, lordJob, _map, new List<Pawn> { pawn });
                }
            }
        }

        private void ScatterMetalDebris(CellRect rect)
        {
            var steelSlagDef = ThingDefOf.ChunkSlagSteel;
            var steelDef = ThingDefOf.Steel;
            var componentDef = ThingDefOf.ComponentIndustrial;

            int debrisCount = Rand.RangeInclusive(5, 12);
            for (int i = 0; i < debrisCount; i++)
            {
                var cell = CellFinder.RandomClosewalkCellNear(rect.CenterCell, _map, rect.Width / 2 + 5);
                if (!cell.InBounds(_map) || !cell.Standable(_map)) continue;

                ThingDef defToSpawn;
                float rand = Rand.Value;
                if (rand < 0.5f) defToSpawn = steelSlagDef;
                else if (rand < 0.8f) defToSpawn = steelDef;
                else defToSpawn = componentDef;

                int stackCount = defToSpawn == steelDef ? Rand.RangeInclusive(20, 50) :
                                defToSpawn == componentDef ? Rand.RangeInclusive(2, 6) : 1;

                var thing = ThingMaker.MakeThing(defToSpawn);
                thing.stackCount = stackCount;
                GenPlace.TryPlaceThing(thing, cell, _map, ThingPlaceMode.Near);
            }
        }

        private void SpawnBloodFilth(CellRect rect)
        {
            int bloodCount = Rand.RangeInclusive(15, 30);
            var bloodDef = ThingDefOf.Filth_Blood;

            for (int i = 0; i < bloodCount; i++)
            {
                var cell = CellFinder.RandomClosewalkCellNear(rect.CenterCell, _map, rect.Width / 2 + 3);
                if (!cell.InBounds(_map)) continue;

                var filth = (Filth)ThingMaker.MakeThing(bloodDef);
                filth.thickness = Rand.RangeInclusive(1, 3);
                GenPlace.TryPlaceThing(filth, cell, _map, ThingPlaceMode.Near);
            }
        }
    }
}