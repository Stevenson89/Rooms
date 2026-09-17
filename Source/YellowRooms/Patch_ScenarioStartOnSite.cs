using RimWorld;
using RimWorld.Planet;
using Verse;

namespace arsiy.Rooms
{
    // During new-game init the vanilla flow creates the player base from the
    // layer's settlementWorldObjectDef (must be a Settlement subclass, because
    // Game.InitNewGame only searches WorldObjects.Settlements for the base).
    // This prefix swaps that temporary Settlement for the mod's YellowRoomsSite
    // (YellowRoomsMapParent) right before map generation, so the started game's
    // home is the mod's own "Yellow Rooms location" world object — identical to
    // settling in the Yellow Rooms from a caravan mid-game.
    public static class Patch_ScenarioStartOnSite
    {
        private const string LayerDefName = "YellowRooms";
        private const string HomeSettlementDefName = "YellowRoomsHomeSettlement";
        private const string SiteDefName = "YellowRoomsSite";

        public static void Prefix(ref MapParent parent)
        {
            if (Find.GameInitData == null) return;
            if (!(parent is Settlement settlement)) return;
            if (settlement.def.defName != HomeSettlementDefName) return;
            if (settlement.Faction != Faction.OfPlayer) return;

            var tile = settlement.Tile;
            if (tile.Layer?.Def == null || tile.Layer.Def.defName != LayerDefName) return;

            var siteDef = DefDatabase<WorldObjectDef>.GetNamedSilentFail(SiteDefName);
            if (siteDef == null)
            {
                RoomsLog.Warning("[Rooms] YellowRoomsSite def not found; starting on a settlement instead.");
                return;
            }

            var site = (MapParent)WorldObjectMaker.MakeWorldObject(siteDef);
            site.Tile = tile;
            site.SetFaction(Faction.OfPlayer);
            Find.WorldObjects.Add(site);
            Find.WorldObjects.Remove(settlement);
            parent = site;
            RoomsLog.Message("[Rooms] Scenario start: swapped home settlement for YellowRoomsSite.");
        }
    }
}
