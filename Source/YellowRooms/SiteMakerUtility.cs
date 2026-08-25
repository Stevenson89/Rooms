using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace arsiy.Rooms
{
    
    
    
    public static class SiteMakerUtility
    {
        
        
        
        
        
        
        public static bool TryFindRandomSiteTile_YellowRooms(out int tile, int minDistance = 3, int maxDistance = 10)
        {
            tile = -1;
            var layerDef = DefDatabase<PlanetLayerDef>.GetNamedSilentFail("YellowRooms");
            if (layerDef == null) return false;
            var layer = Find.WorldGrid.FirstLayerOfDef(layerDef);
            if (layer == null || layer.TilesCount == 0) return false;

            int rootTile = -1;
            var playerHomeMap = Find.AnyPlayerHomeMap;
            if (playerHomeMap != null)
                rootTile = playerHomeMap.Tile;

            tile = Rand.Range(0, layer.TilesCount);
            return tile >= 0;
        }

        
        
        
        
        
        public static bool TryFindRandomSitePlanetTile_YellowRooms(out PlanetTile tile, int minDistance = 3, int maxDistance = 15)
        {
            tile = PlanetTile.Invalid;
            var layerDef = DefDatabase<PlanetLayerDef>.GetNamedSilentFail("YellowRooms");
            if (layerDef == null) return false;
            var layer = Find.WorldGrid.FirstLayerOfDef(layerDef);
            if (layer == null || layer.TilesCount == 0) return false;

            tile = layer.PlanetTileForID(Rand.Range(0, layer.TilesCount));
            return tile.Valid;
        }

        
        
        
        
public static List<Pawn> SpawnFactionGroup(Faction faction, Map map, float points,
            IntVec3 nearCenter, int radius = 25, int minCount = 0)
        {
            var result = new List<Pawn>();
            if (faction == null || faction.def?.pawnGroupMakers == null || faction.def.pawnGroupMakers.Count == 0)
                return result;

            
            
            
            
            
            float minCost = MinCombatPawnCost(faction.def);
            if (points < minCost) points = minCost;

            var parms = new PawnGroupMakerParms
            {
                faction = faction,
                groupKind = PawnGroupKindDefOf.Combat,
                points = points,
                tile = map.Tile,
                raidStrategy = RaidStrategyDefOf.ImmediateAttack
            };

            IEnumerable<Pawn> group;
            try
            {
                group = PawnGroupMakerUtility.GeneratePawns(parms, warnOnZeroResults: false);
            }
            catch (System.Exception ex)
            {
                RoomsLog.Warning($"[Rooms] SpawnFactionGroup failed to generate pawns: {ex.Message}");
                return result;
            }

            foreach (var pawn in group)
            {
                var cell = CellFinder.RandomClosewalkCellNear(nearCenter, map, radius);
                GenSpawn.Spawn(pawn, cell, map, Rot4.Random);
                result.Add(pawn);
            }

            int guard = 40;
            while (result.Count < minCount && guard-- > 0)
            {
                var topParms = new PawnGroupMakerParms
                {
                    faction = faction,
                    groupKind = PawnGroupKindDefOf.Combat,
                    points = minCost,
                    tile = map.Tile,
                    raidStrategy = RaidStrategyDefOf.ImmediateAttack
                };

                bool any = false;
                IEnumerable<Pawn> topGroup;
                try
                {
                    topGroup = PawnGroupMakerUtility.GeneratePawns(topParms, warnOnZeroResults: false);
                }
                catch
                {
                    break;
                }

                foreach (var pawn in topGroup)
                {
                    any = true;
                    var cell = CellFinder.RandomClosewalkCellNear(nearCenter, map, radius);
                    GenSpawn.Spawn(pawn, cell, map, Rot4.Random);
                    result.Add(pawn);
                }
                if (!any) break;
            }
            return result;
        }

        
        
        private static float MinCombatPawnCost(FactionDef def)
        {
            float min = float.MaxValue;
            if (def?.pawnGroupMakers != null)
            {
                foreach (var maker in def.pawnGroupMakers)
                {
                    if (maker.kindDef != PawnGroupKindDefOf.Combat || maker.options == null)
                        continue;
                    foreach (var opt in maker.options)
                    {
                        if (opt?.kind != null && opt.kind.combatPower > 0f && opt.kind.combatPower < min)
                            min = opt.kind.combatPower;
                    }
                }
            }
            return min < float.MaxValue ? min : 50f;
        }
    }
}
