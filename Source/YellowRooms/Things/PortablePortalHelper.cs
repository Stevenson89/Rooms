using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace arsiy.Rooms.Things
{
    
    
    
    
    
    
    
    public static class PortablePortalHelper
    {
        public static bool TryTeleport(Pawn usedBy)
        {
            if (usedBy == null || usedBy.Map == null) return false;

            bool fromYellowRooms = IsYellowRoomsMap(usedBy.Map);
            Map targetMap = FindDestinationMap(fromYellowRooms);
            if (targetMap == null)
            {
                Messages.Message("PortablePortalNoDestination".Translate(
                    "No destination available for the portable portal."),
                    usedBy, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            var landingCell = CellFinder.RandomClosewalkCellNear(targetMap.Center, targetMap, 30);
            if (!landingCell.IsValid || !landingCell.Standable(targetMap))
            {
                
                
                landingCell = CellFinder.RandomClosewalkCellNear(targetMap.Center, targetMap, 50);
                if (!landingCell.IsValid || !landingCell.Standable(targetMap))
                {
                    foreach (var c in targetMap.AllCells)
                    {
                        if (c.Standable(targetMap) && c.GetEdifice(targetMap) == null)
                        { landingCell = c; break; }
                    }
                }
            }

            LongEventHandler.QueueLongEvent(() =>
            {
                usedBy.DeSpawn();
                GenSpawn.Spawn(usedBy, landingCell, targetMap, Rot4.Random);
                Current.Game.CurrentMap = targetMap;
                CameraJumper.TryJumpAndSelect(usedBy);
            }, "PortablePortalTravelling".Translate("Travelling..."), doAsynchronously: false, null);
            return true;
        }

        
        public static bool IsYellowRoomsMap(Map map)
        {
            if (map == null) return false;
            
            
            var yrBiome = DefDatabase<BiomeDef>.GetNamedSilentFail("YellowRooms");
            return yrBiome != null && map.Biome == yrBiome;
        }

        
        
        
        
        
        
        
        
        
        private static Map FindDestinationMap(bool fromYellowRooms)
        {
            if (fromYellowRooms)
            {
                var pawnMap = FindSurfacePawnMap();
                if (pawnMap != null) return pawnMap;
                return CreateSurfaceCamp();
            }

            
            Map fallback = null;
            foreach (var map in Find.Maps)
            {
                if (!IsYellowRoomsMap(map)) continue;
                if (map.mapPawns.FreeColonistsSpawnedCount > 0)
                    return map;
                if (fallback == null) fallback = map;
            }
            return fallback ?? CreateYellowRoomsCamp();
        }

        
        private static Map FindSurfacePawnMap()
        {
            foreach (var map in Find.Maps)
            {
                if (IsYellowRoomsMap(map)) continue;
                if (map.mapPawns.FreeColonistsSpawnedCount > 0)
                    return map;
            }
            return null;
        }

        
        
        
        
        
        public static Map CreateSurfaceCamp()
        {
            return CreateCamp("Surface");
        }

        
        
        
        
        
        private static Map CreateYellowRoomsCamp()
        {
            return CreateCamp("YellowRooms");
        }

        private static Map CreateCamp(string layerDefName)
        {
            var layerDef = DefDatabase<PlanetLayerDef>.GetNamedSilentFail(layerDefName);
            var layer = layerDef == null ? null : Find.WorldGrid.FirstLayerOfDef(layerDef);
            if (layer == null) return null;

            if (!TileFinder.TryFindNewSiteTile(out var tile,
                    minDist: 7, maxDist: 27, allowCaravans: false,
                    allowedLandmarks: null, selectLandmarkChance: 0f,
                    canSelectComboLandmarks: true, tileFinderMode: TileFinderMode.Near,
                    exitOnFirstTileFound: false, canBeSpace: false,
                    layer: layer,
                    validator: t => SettleInEmptyTileUtility.CanCreateMapAt(t)))
                return null;

            IntVec3 size = WorldObjectDefOf.Camp.overrideMapSize ?? Find.World.info.initialMapSize;
            Map map = GetOrGenerateMapUtility.GetOrGenerateMap(tile, size, WorldObjectDefOf.Camp);
            map?.Parent?.SetFaction(Faction.OfPlayer);
            return map;
        }
    }
}
