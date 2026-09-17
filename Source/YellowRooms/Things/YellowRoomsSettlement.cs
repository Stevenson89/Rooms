using RimWorld;
using RimWorld.Planet;
using Verse;

namespace arsiy.Rooms.Things
{
    // Player home in the Yellow Rooms. Must derive from Settlement: the vanilla
    // scenario start (ScenPart_PlayerFaction.PreMapGenerate creates the world
    // object from PlanetLayerDef.settlementWorldObjectDef, Game.InitNewGame then
    // searches WorldObjects.Settlements for the player base) casts to Settlement.
    public class YellowRoomsSettlement : Settlement
    {
        public override MapGeneratorDef MapGeneratorDef
        {
            get
            {
                var biome = Biome;
                string genName = "YellowRooms";
                if (biome != null)
                {
                    if (biome.defName == "YellowRooms_PoolRooms")
                        genName = "YellowRooms_PoolRooms";
                    else if (biome.defName == "YellowRooms_Parking")
                        genName = "YellowRooms_Parking";
                }
                return DefDatabase<MapGeneratorDef>.GetNamedSilentFail(genName);
            }
        }

        public override System.Collections.Generic.IEnumerable<IncidentTargetTagDef> IncidentTargetTags()
        {
            foreach (IncidentTargetTagDef item in base.IncidentTargetTags())
                yield return item;
            if (Faction == Faction.OfPlayer)
                yield return IncidentTargetTagDefOf.Map_PlayerHome;
            else
                yield return IncidentTargetTagDefOf.Map_Misc;
        }
    }
}
