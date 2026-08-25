using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;
using arsiy.Rooms.Things;

namespace arsiy.Rooms.GenSteps
{
    public class GenStep_SurfaceResearcherBase : GenStep
    {
        private const string LayoutDefName = "YellowRooms_SurfaceResearcherBase";
        private const int BaseWidth = 24;
        private const int BaseHeight = 20;

        public override int SeedPart => 28473913;

        public override void Generate(Map map, GenStepParams parms)
        {
            var site = map.Parent as SurfaceResearcherBase;
            var faction = site?.Faction ?? YellowRoomsUtility.GetResearchers();

            if (faction == null)
            {
                RoomsLog.Warning("[Rooms] SurfaceResearcherBase: Researcher faction not found");
                return;
            }

            var layoutDef = DefDatabase<StructureLayoutDef>.GetNamedSilentFail(LayoutDefName);
            if (layoutDef == null)
            {
                RoomsLog.Warning($"[Rooms] SurfaceResearcherBase: layout def {LayoutDefName} not found");
                return;
            }

            var center = map.Center;
            var size = new IntVec2(BaseWidth, BaseHeight);
            var pos = new IntVec3(center.x - size.x / 2, 0, center.z - size.z / 2);

            var layoutParms = new StructureGenParams { size = size, faction = faction };
            var sketch = layoutDef.Worker.GenerateStructureSketch(layoutParms);
            layoutDef.Worker.Spawn(sketch, map, pos, null, new List<Thing>(), true, false, faction);

            float points = StorytellerUtility.DefaultThreatPointsNow(map);
            var pawns = SiteMakerUtility.SpawnFactionGroup(faction, map, points, center, 10, 7);
            if (pawns.Count == 0) return;

            LordMaker.MakeNewLord(faction, new LordJob_AssaultColony(faction, true, true, false, true), map, pawns);
        }
    }
}