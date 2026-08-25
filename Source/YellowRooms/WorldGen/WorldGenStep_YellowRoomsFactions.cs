using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace arsiy.Rooms.WorldGen
{
    public class WorldGenStep_YellowRoomsFactions : WorldGenStep
    {
        public override int SeedPart => 987654321;

        public override void GenerateFresh(string seed, PlanetLayer layer)
        {
            var layerDef = DefDatabase<PlanetLayerDef>.GetNamedSilentFail("YellowRooms");
            if (layerDef == null) return;

            var yellowRoomsLayer = Find.WorldGrid.FirstLayerOfDef(layerDef);
            if (yellowRoomsLayer == null) return;

            CreateFactionIfMissing("YellowRooms_Researchers");
            CreateFactionIfMissing("YellowRooms_StillLifeFaction");
            CreateFactionIfMissing("YellowRooms_Survivors");
            CreateFactionIfMissing("YellowRooms_SurvivorTraders");
        }

        private void CreateFactionIfMissing(string defName)
        {
            if (Find.FactionManager.AllFactions.Any(f => f.def?.defName == defName))
                return;

            var def = DefDatabase<FactionDef>.GetNamedSilentFail(defName);
            if (def == null)
            {
                RoomsLog.Warning($"[Rooms] FactionDef {defName} not found during world gen");
                return;
            }

            var faction = FactionGenerator.NewGeneratedFaction(new FactionGeneratorParms(def, hidden: true));
            Find.FactionManager.Add(faction);
            RoomsLog.Message($"[Rooms] Created hidden faction at world gen: {defName}");
        }
    }
}