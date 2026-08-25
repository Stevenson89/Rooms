using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace arsiy.Rooms.GenSteps
{
    public class GenStep_ResearcherBase : GenStep
    {
        private const string LayoutDefName = "YellowRooms_ResearcherBase";
        private const int BaseWidth = 32;
        private const int BaseHeight = 26;
        private const int ClearMargin = 6;
        private Map _map;

        public override int SeedPart => 28473914;

        public override void Generate(Map map, GenStepParams parms)
        {
            _map = map;
            var researcherFaction = YellowRoomsUtility.GetResearchers();

            if (researcherFaction == null)
            {
                RoomsLog.Warning("[Rooms] ResearcherBase: Researcher faction not found");
                return;
            }

            var layoutDef = DefDatabase<StructureLayoutDef>.GetNamedSilentFail(LayoutDefName);
            if (layoutDef == null)
            {
                RoomsLog.Warning($"[Rooms] ResearcherBase: layout def {LayoutDefName} not found");
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

            SpawnLivingResearchers(center, researcherFaction);
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

        private void SpawnLivingResearchers(IntVec3 center, Faction researcherFaction)
        {
            float points = StorytellerUtility.DefaultThreatPointsNow(_map);
            var pawns = SiteMakerUtility.SpawnFactionGroup(researcherFaction, _map, points, center, 16, 7);
            if (pawns.Count == 0) return;

            LordMaker.MakeNewLord(researcherFaction, new LordJob_DefendPoint(center, 30f), _map, pawns);
        }
    }
}