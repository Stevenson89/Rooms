using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace arsiy.Rooms
{
    
    
    
    
    public static class YellowRoomsUtility
    {
        
        private static readonly HashSet<string> YellowBiomeNames = new HashSet<string>
        {
            "YellowRooms", "YellowRooms_PoolRooms", "YellowRooms_Parking"
        };

        
        public static bool IsYellowRoomsMap(Map map)
        {
            return map != null && map.Biome != null && YellowBiomeNames.Contains(map.Biome.defName);
        }

        
        public static bool IsYellowRoomsBiome(BiomeDef biome)
        {
            return biome != null && YellowBiomeNames.Contains(biome.defName);
        }

        public static bool IsYellowRoomsCaravan(Caravan caravan)
        {
            if (caravan == null || !caravan.IsPlayerControlled) return false;
            var layerDef = DefDatabase<PlanetLayerDef>.GetNamedSilentFail("YellowRooms");
            if (layerDef == null) return false;
            var layer = Find.WorldGrid.FirstLayerOfDef(layerDef);
            return layer != null && caravan.Tile.Layer == layer;
        }

        
        
        public static Faction GetResearchers()
            => GetOrCreateFaction("YellowRooms_Researchers");

        public static Faction GetStillLifeFaction()
            => GetOrCreateFaction("YellowRooms_StillLifeFaction");

        public static Faction GetPassiveStillLifeFaction()
            => GetOrCreateFaction("YellowRooms_PassiveStillLifeFaction", forceHostile: false);

        public static Faction GetSurvivors()
            => GetOrCreateFaction("YellowRooms_Survivors");

        
        
        public static Faction GetSurvivorTraders()
            => GetOrCreateFaction("YellowRooms_SurvivorTraders", forceHostile: false);

        private static Faction GetOrCreateFaction(string defName, bool forceHostile = true)
        {
            var existing = Find.FactionManager?.AllFactions
                .FirstOrDefault(f => f.def?.defName == defName);
            if (existing != null) return existing;

            var def = DefDatabase<FactionDef>.GetNamedSilentFail(defName);
            if (def == null) return null;

            var faction = FactionGenerator.NewGeneratedFaction(
                new FactionGeneratorParms(def, hidden: true));
            Find.FactionManager.Add(faction);
            if (forceHostile)
                faction.SetRelationDirect(Faction.OfPlayer, FactionRelationKind.Hostile);
            return faction;
        }

        
        public static ThingDef PitDef => ThingDef.Named("YellowRooms_Pit");
        public static ThingDef CeilingHoleDef => ThingDef.Named("YellowRooms_CeilingHole");

        
        public static List<Thing> GetPits(Map map)
        {
            var def = PitDef;
            return def == null ? new List<Thing>() : map.listerThings.ThingsOfDef(def);
        }

        
        public static List<Thing> GetCeilingHoles(Map map)
        {
            var def = CeilingHoleDef;
            return def == null ? new List<Thing>() : map.listerThings.ThingsOfDef(def);
        }

        
        
        
        
        
        public static int StillLifeCountFor(float points)
        {
            int n = GenMath.RoundRandom(points / 200f);
            if (n < 1) n = 1;
            if (n > 20) n = 20;
            return n;
        }

        
        public static PawnKindDef RandomStillLifeKind()
        {
            var pool = new[]
            {
                PawnKindDef.Named("YellowRooms_StillLife_Drifter"),
                PawnKindDef.Named("YellowRooms_StillLife_Melee")
            };
            return pool.RandomElement();
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
        public static Pawn SpawnHostilePawn(PawnKindDef kind, Faction faction, IntVec3 cell, Map map)
        {
            if (kind == null || faction == null) return null;

            bool asStillLife = kind.defName == "YellowRooms_StillLife_Drifter"
                            || kind.defName == "YellowRooms_StillLife_Melee";

            Pawn pawn;
            if (asStillLife)
            {
                
                
                
                var req = new PawnGenerationRequest(
                    kind, null, PawnGenerationContext.NonPlayer,
                    tile: map.Tile, forceGenerateNewPawn: true,
                    allowDowned: false, colonistRelationChanceFactor: 0f,
                    forceNoIdeo: true);
                pawn = PawnGenerator.GeneratePawn(req);
                if (pawn != null)
                    pawn.SetFaction(faction);
            }
            else
            {
                var req = new PawnGenerationRequest(
                    kind, faction, PawnGenerationContext.PlayerStarter,
                    tile: map.Tile, forceGenerateNewPawn: true,
                    allowDowned: false, colonistRelationChanceFactor: 0f);
                pawn = PawnGenerator.GeneratePawn(req);
            }

            if (pawn == null) return null;
            GenSpawn.Spawn(pawn, cell, map, Rot4.Random);

            var stillLifeDef = DefDatabase<HediffDef>.GetNamedSilentFail("YellowRooms_StillLife");
            if (stillLifeDef != null && pawn.health?.hediffSet?.HasHediff(stillLifeDef) == true)
            {
                arsiy.Rooms.Incidents.CloneHelper.ApplyMissingParts(pawn);
                arsiy.Rooms.Incidents.CloneHelper.ApplyRandomModifications(pawn);
            }

            return pawn;
        }

        
        public static bool TryFindRaidEdgeCell(Map map, out IntVec3 cell)
        {
            if (CellFinder.TryFindRandomEdgeCellWith(c => c.Standable(map), map, CellFinder.EdgeRoadChance_Ignore, out cell))
                return true;
            if (RCellFinder.TryFindRandomPawnEntryCell(out cell, map, 0f, false, null))
                return true;
            cell = CellFinder.RandomClosewalkCellNear(map.Center, map, 30);
            return cell.IsValid;
        }

        
        
        
        public static PawnKindDef ChooseKindByPoints(float points, params string[] kindDefNames)
        {
            var kinds = kindDefNames.Select(PawnKindDef.Named).Where(k => k != null).ToList();
            if (kinds.Count == 0) return null;
            if (kinds.Count == 1) return kinds[0];

            
            float pointsPerKind = 60f;
            int maxIndex = Mathf.Min(kinds.Count - 1, Mathf.FloorToInt(points / pointsPerKind));
            int index = Rand.RangeInclusive(0, maxIndex);
            return kinds[index];
        }

        
        
        
        
        public static List<Pawn> SpawnGroupAtEdge(Faction faction, Map map, float points, params string[] kindDefNames)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(points / 50f));
            var result = new List<Pawn>();

            TryFindRaidEdgeCell(map, out var entryCell);

            for (int i = 0; i < count; i++)
            {
                var kindDef = ChooseKindByPoints(points, kindDefNames);
                if (kindDef == null) continue;

                var req = new PawnGenerationRequest(kindDef, faction, PawnGenerationContext.NonPlayer,
                    tile: map.Tile, forceGenerateNewPawn: true);
                var pawn = PawnGenerator.GeneratePawn(req);
                if (pawn == null) continue;

                var cell = CellFinder.RandomClosewalkCellNear(entryCell, map, 5);
                GenSpawn.Spawn(pawn, cell, map, Rot4.Random);
                result.Add(pawn);
            }
            return result;
        }

        
        private static float MinPawnCostFor(Faction faction, PawnGroupKindDef groupKind)
        {
            float min = float.MaxValue;
            if (faction.def.pawnGroupMakers == null) return 0f;
            foreach (var maker in faction.def.pawnGroupMakers)
            {
                if (maker.kindDef != groupKind) continue;
                foreach (var option in maker.options)
                {
                    var kind = option.kind;
                    if (kind != null && kind.combatPower < min)
                        min = kind.combatPower;
                }
            }
            return min == float.MaxValue ? 0f : min;
        }

        
        
        
        
        
        public static List<Pawn> SpawnGroupAtEdgeVanilla(Faction faction, Map map, float points)
        {
            float minCost = MinPawnCostFor(faction, PawnGroupKindDefOf.Combat);
            if (minCost > 0f && points < minCost)
                points = minCost;

            var parms = new PawnGroupMakerParms
            {
                points = points,
                faction = faction,
                groupKind = PawnGroupKindDefOf.Combat,
                generateFightersOnly = true,
                raidStrategy = RaidStrategyDefOf.ImmediateAttack
            };

            var pawns = PawnGroupMakerUtility.GeneratePawns(parms).ToList();
            if (pawns.Count == 0) return pawns;

            TryFindRaidEdgeCell(map, out var entryCell);

            for (int i = 0; i < pawns.Count; i++)
            {
                var cell = CellFinder.RandomClosewalkCellNear(entryCell, map, Mathf.Min(8, 2 + i * 2));
                GenSpawn.Spawn(pawns[i], cell, map, Rot4.Random);
            }

            return pawns;
        }

        
        
        
        
        public static bool TryGetRandomHumanlikeFaction(out Faction result, bool excludePlayer = true)
        {
            var pool = Find.FactionManager.AllFactionsInViewOrder
                .Where(f => !f.def.defName.StartsWith("YellowRooms_")
                         && f.def.humanlikeFaction
                         && f.HostileTo(Faction.OfPlayer)
                         && (!excludePlayer || f != Faction.OfPlayer))
                .ToList();

            if (pool.Count == 0)
            {
                result = null;
                return false;
            }

            result = pool.RandomElement();
            return true;
        }

        
        
        
        
        
        public static List<Pawn> SpawnStillLifeGroupAtEdge(Faction raidFaction, Faction stillLifeFaction, Map map, float points)
        {
            var pawns = SpawnGroupAtEdgeVanilla(raidFaction, map, points);
            if (pawns.Count == 0) return pawns;

            var stillLifeDef = DefDatabase<HediffDef>.GetNamedSilentFail("YellowRooms_StillLife");

            foreach (var pawn in pawns)
            {
                pawn.SetFaction(stillLifeFaction);

                if (stillLifeDef != null && !pawn.health.hediffSet.HasHediff(stillLifeDef))
                {
                    pawn.health.AddHediff(stillLifeDef);
                    arsiy.Rooms.Incidents.CloneHelper.ApplyMissingParts(pawn);
                    arsiy.Rooms.Incidents.CloneHelper.ApplyRandomModifications(pawn);
                }
            }

            return pawns;
        }

        
        
        
        
        
        
        
        
        
        
        public static bool TryForceNearestResearcherKidnap(Map map)
        {
            if (map == null) return false;
            var researchers = GetResearchers();
            if (researchers == null) return false;

            var downed = new List<Pawn>();
            var kidnappers = new List<Pawn>();
            foreach (var p in map.mapPawns.AllPawnsSpawned)
            {
                if (p.Faction == Faction.OfPlayer && p.RaceProps.Humanlike && p.Downed)
                {
                    downed.Add(p);
                }
                else if (p.Faction == researchers
                         && p.Spawned && !p.Downed
                         && p.MentalStateDef == null
                         && p.carryTracker?.CarriedThing == null
                         && p.jobs?.curJob?.def != JobDefOf.Kidnap
                         && p.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                {
                    kidnappers.Add(p);
                }
            }
            if (downed.Count == 0 || kidnappers.Count == 0) return false;

            
            Pawn bestKidnapper = null;
            float bestDistSq = float.MaxValue;
            foreach (var k in kidnappers)
            {
                foreach (var v in downed)
                {
                    float sq = k.Position.DistanceToSquared(v.Position);
                    if (sq < bestDistSq)
                    {
                        bestDistSq = sq;
                        bestKidnapper = k;
                    }
                }
            }
            if (bestKidnapper == null) return false;

            
            
            
            if (!RCellFinder.TryFindBestExitSpot(bestKidnapper, out var exitSpot)) return false;
            if (!KidnapAIUtility.TryFindGoodKidnapVictim(bestKidnapper, 9999f, out var victim)) return false;

            var job = JobMaker.MakeJob(JobDefOf.Kidnap, victim, exitSpot);
            job.count = 1;
            bestKidnapper.jobs.StartJob(job, JobCondition.InterruptForced, null,
                resumeCurJobAfterwards: false, cancelBusyStances: true,
                thinkTree: null, tag: JobTag.Misc);
            return true;
        }
    }
}
