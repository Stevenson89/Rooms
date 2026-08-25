using RimWorld;
using RimWorld.Planet;
using Verse;
using arsiy.Rooms.Things;

namespace arsiy.Rooms
{
    public static class Patch_SettleInYellowRooms
    {
        private const string LayerDefName = "YellowRooms";
        private const string SiteDefName = "YellowRoomsSite";

        public static bool Prefix(Caravan caravan)
        {
            if (caravan == null || caravan.Faction != Faction.OfPlayer) return true;

            var layerDef = caravan.Tile.Layer?.Def;
            if (layerDef == null || layerDef.defName != LayerDefName) return true;

            var siteDef = DefDatabase<WorldObjectDef>.GetNamedSilentFail(SiteDefName);
            if (siteDef == null)
            {
                RoomsLog.Error($"[Rooms] WorldObjectDef '{SiteDefName}' not found; falling back to vanilla settle.");
                return true;
            }

            var site = (YellowRoomsMapParent)WorldObjectMaker.MakeWorldObject(siteDef);
            site.Tile = caravan.Tile;
            site.SetFaction(caravan.Faction);
            Find.WorldObjects.Add(site);

            LongEventHandler.QueueLongEvent(delegate
            {
                var map = site.GenerateYellowRoomsMap();
                if (map == null)
                {
                    RoomsLog.Error("[Rooms] Failed to generate YellowRooms map during settle; removing site.");
                    if (site.HasMap)
                        Current.Game.DeinitAndRemoveMap(site.Map, notifyPlayer: false);
                    Find.WorldObjects.Remove(site);
                    return;
                }
                var pawn = caravan.PawnsListForReading.Count > 0 ? caravan.PawnsListForReading[0] : null;
                CaravanEnterMapUtility.Enter(caravan, map, CaravanEnterMode.Center, CaravanDropInventoryMode.DropInstantly, draftColonists: false, (IntVec3 x) => x.GetRoom(map).CellCount >= 600);
                site.Notify_MyMapSettled(map);
                if (pawn != null)
                    CameraJumper.TryJump(pawn);
            }, "GeneratingMap", doAsynchronously: true, GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap);

            return false;
        }
    }
}