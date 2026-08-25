using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace arsiy.Rooms.Incidents
{
    
    
    
    
    
    public class IncidentWorker_SurvivorCampNearby : IncidentWorker_YellowRoomsBase
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            return YellowRoomsUtility.GetSurvivors() != null
                && SiteMakerUtility.TryFindRandomSitePlanetTile_YellowRooms(out _, 3, 15);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            var faction = YellowRoomsUtility.GetSurvivors();
            if (faction == null) return false;
            if (!SiteMakerUtility.TryFindRandomSitePlanetTile_YellowRooms(out var tile, 3, 15))
                return false;

            float points = parms.points > 0 ? parms.points : StorytellerUtility.DefaultSiteThreatPointsNow();

            
            
            
            points = Mathf.Max(points, 50f);

            
            
            var sitePartDef = DefDatabase<SitePartDef>.GetNamedSilentFail("YellowRooms_BanditCamp")
                ?? SitePartDefOf.BanditCamp;

            var site = SiteMaker.MakeSite(sitePartDef, tile, faction,
                ifHostileThenMustRemainHostile: true, threatPoints: points);
            if (site == null) return false;
            Find.WorldObjects.Add(site);

            var letter = LetterMaker.MakeLetter("YellowRooms_Letter_SurvivorCamp_Title".Translate(),
                "YellowRooms_Letter_SurvivorCamp_Text".Translate(),
                LetterDefOf.ThreatBig, site);
            Find.LetterStack.ReceiveLetter(letter);
            return true;
        }
    }

    
    
    
    public class IncidentWorker_HiveFromCeiling : IncidentWorker_YellowRoomsBase
    {
        private static readonly IntRange HiveCount = new IntRange(1, 4);

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            var map = (Map)parms.target;
            return YellowRoomsUtility.GetCeilingHoles(map).Count > 0;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            var map = (Map)parms.target;
            var holes = YellowRoomsUtility.GetCeilingHoles(map);
            if (holes.Count == 0) return false;
            var hole = holes.RandomElement();

            int n = HiveCount.RandomInRange;
            var hiveDef = ThingDefOf.Hive;
            var spawned = new List<Thing>();
            for (int i = 0; i < n; i++)
            {
                var cell = CellFinder.RandomClosewalkCellNear(hole.Position, map, 4);
                var hive = GenSpawn.Spawn(hiveDef, cell, map, WipeMode.Vanish);
                if (hive != null) spawned.Add(hive);
            }
            if (spawned.Count == 0) return false;

            var letter = LetterMaker.MakeLetter("YellowRooms_Letter_HiveFromCeiling_Title".Translate(),
                "YellowRooms_Letter_HiveFromCeiling_Text".Translate(),
                LetterDefOf.ThreatBig, hole);
            Find.LetterStack.ReceiveLetter(letter);
            return true;
        }
    }


    
    
    
    public class IncidentWorker_BirdFromCeiling : IncidentWorker_YellowRoomsBase
    {
private static readonly string[] BirdDefNames =
        {
            "Chicken", "Duck", "Goose", "Cassowary", "Emu", "Ostrich", "Turkey"
        };

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            var map = (Map)parms.target;
            return YellowRoomsUtility.GetCeilingHoles(map).Count > 0;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            var map = (Map)parms.target;
            var holes = YellowRoomsUtility.GetCeilingHoles(map);
            if (holes.Count == 0) return false;
            var hole = holes.RandomElement();

            var birdKindDefName = BirdDefNames.RandomElement();
            var pawnKindDef = PawnKindDef.Named(birdKindDefName);
            if (pawnKindDef == null) return false;

            var req = new PawnGenerationRequest(pawnKindDef, null,
                PawnGenerationContext.PlayerStarter, tile: map.Tile, forceGenerateNewPawn: true);
            var bird = PawnGenerator.GeneratePawn(req);

            
            var cell = CellFinder.RandomClosewalkCellNear(hole.Position, map, 1);
            GenSpawn.Spawn(bird, cell, map, Rot4.Random);

            var letter = LetterMaker.MakeLetter("YellowRooms_Letter_BirdFromCeiling_Title".Translate(),
                "YellowRooms_Letter_BirdFromCeiling_Text".Translate(birdKindDefName.ToLower()),
                LetterDefOf.NeutralEvent, bird);
            Find.LetterStack.ReceiveLetter(letter);
            return true;
        }
    }

    
    
    
    
    
    public class IncidentWorker_OreFromCeiling : IncidentWorker_YellowRoomsBase
    {
        
        
        private static ThingDef RandomOreDef()
        {
            var ores = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(d => d.building != null
                         && d.building.isResourceRock
                         && d.building.mineableThing != null)
                .ToList();

            if (ores.Count == 0)
                return ThingDef.Named("MineableComponentsIndustrial");
            return ores.RandomElement();
        }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            var map = (Map)parms.target;
            return YellowRoomsUtility.GetCeilingHoles(map).Count > 0;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            var map = (Map)parms.target;
            var holes = YellowRoomsUtility.GetCeilingHoles(map);
            if (holes.Count == 0) return false;
            var hole = holes.RandomElement();

            
            var oreDef = RandomOreDef();
            if (oreDef == null) return false;

            int spawned = 0;
            IntVec3 firstCell = IntVec3.Invalid;
            var cells = new List<IntVec3>();
            
            
            for (int x = 0; x <= 1; x++)
            {
                for (int z = 0; z <= 1; z++)
                {
                    var cell = hole.Position + new IntVec3(x, 0, z);
                    if (cell.InBounds(map) && cell.Standable(map) && cell.GetEdifice(map) == null)
                        cells.Add(cell);
                }
            }
            
            
            if (cells.Count < 4)
            {
                cells.Clear();
                for (int radius = 1; radius <= 4; radius++)
                {
                    foreach (var c in GenRadial.RadialCellsAround(hole.Position, radius, true))
                    {
                        if (c.InBounds(map) && c.Standable(map) && c.GetEdifice(map) == null)
                            cells.Add(c);
                        if (cells.Count >= 4) break;
                    }
                    if (cells.Count >= 4) break;
                }
            }

            for (int i = 0; i < 4 && i < cells.Count; i++)
            {
                GenSpawn.Spawn(oreDef, cells[i], map, Rot4.North, WipeMode.Vanish);
                spawned++;
                if (!firstCell.IsValid) firstCell = cells[i];
            }

            if (spawned == 0)
            {
                
                firstCell = hole.Position;
                GenSpawn.Spawn(oreDef, firstCell, map, Rot4.North, WipeMode.Vanish);
                spawned = 1;
            }

            var letter = LetterMaker.MakeLetter("YellowRooms_Letter_OreFromCeiling_Title".Translate(),
                "YellowRooms_Letter_OreFromCeiling_Text".Translate(spawned),
                LetterDefOf.PositiveEvent, new TargetInfo(firstCell, map));
            Find.LetterStack.ReceiveLetter(letter);
            return true;
        }
    }

    
    
    
    
    
    
    
    public class IncidentWorker_StillLifeRaid : IncidentWorker_YellowRoomsBase
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            var map = parms.target as Map;
            if (map == null || !YellowRoomsUtility.IsYellowRoomsMap(map)) return false;
            return YellowRoomsUtility.GetStillLifeFaction() != null
                && YellowRoomsUtility.TryGetRandomHumanlikeFaction(out _, false);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            var map = (Map)parms.target;
            var stillLifeFaction = YellowRoomsUtility.GetStillLifeFaction();
            if (stillLifeFaction == null) return false;

            if (!YellowRoomsUtility.TryGetRandomHumanlikeFaction(out var raidFaction, false))
                return false;

            float points = parms.points > 0 ? parms.points : StorytellerUtility.DefaultThreatPointsNow(map);

            var pawns = YellowRoomsUtility.SpawnStillLifeGroupAtEdge(raidFaction, stillLifeFaction, map, points);
            if (pawns.Count == 0) return false;

            var lordJob = new LordJob_AssaultColony(stillLifeFaction, canKidnap: true, canTimeoutOrFlee: true, sappers: false, useAvoidGridSmart: true);
            LordMaker.MakeNewLord(stillLifeFaction, lordJob, map, pawns);

            var letter = LetterMaker.MakeLetter("YellowRooms_Letter_StillLifeRaid_Title".Translate(),
                "YellowRooms_Letter_StillLifeRaid_Text".Translate(pawns.Count),
                LetterDefOf.ThreatBig, pawns[0]);
            Find.LetterStack.ReceiveLetter(letter);
            return true;
        }
    }

    
    
    
    
    
    public class IncidentWorker_SurvivorRaid : IncidentWorker_YellowRoomsBase
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            var map = parms.target as Map;
            if (map == null || !YellowRoomsUtility.IsYellowRoomsMap(map)) return false;
            return YellowRoomsUtility.GetSurvivors() != null;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            var map = (Map)parms.target;
            var faction = YellowRoomsUtility.GetSurvivors();
            if (faction == null) return false;

            float points = parms.points > 0 ? parms.points : StorytellerUtility.DefaultThreatPointsNow(map);

            var pawns = YellowRoomsUtility.SpawnGroupAtEdgeVanilla(faction, map, points);

            
            
            int fromQueue = 0;
            var queued = map.GetComponent<arsiy.Rooms.Things.YellowRoomsMapComponent>()?.TakePitSurvivors();
            if (queued != null)
            {
                foreach (var survivor in queued)
                {
                    if (survivor == null || survivor.Destroyed) continue;
                    if (survivor.Faction != faction)
                        survivor.SetFaction(faction);
                    if (!survivor.Spawned)
                    {
                        var cell = CellFinder.RandomClosewalkCellNear(map.Center, map, 20);
                        GenSpawn.Spawn(survivor, cell, map, Rot4.Random);
                    }
                    pawns.Add(survivor);
                    fromQueue++;
                }
            }

            if (pawns.Count == 0) return false;

            var lordJob = new LordJob_AssaultColony(faction, canKidnap: true, canTimeoutOrFlee: true, sappers: false, useAvoidGridSmart: true);
            LordMaker.MakeNewLord(faction, lordJob, map, pawns);

            var letter = LetterMaker.MakeLetter("YellowRooms_Letter_SurvivorRaid_Title".Translate(),
                "YellowRooms_Letter_SurvivorRaid_Text".Translate(pawns.Count, fromQueue),
                LetterDefOf.ThreatBig, pawns[0]);
            if (fromQueue > 0)
            {
                Find.LetterStack.ReceiveLetter(LetterMaker.MakeLetter(
                    "YellowRooms_Letter_SurvivorsFromPit_Title".Translate(),
                    "YellowRooms_Letter_SurvivorsFromPit_Text".Translate(fromQueue),
                    LetterDefOf.NegativeEvent, pawns[0]));
            }
            Find.LetterStack.ReceiveLetter(letter);
            return true;
        }
    }

    
    
    
    
    public class IncidentWorker_ResearcherRaid : IncidentWorker_YellowRoomsBase
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            var map = parms.target as Map;
            if (map == null || !YellowRoomsUtility.IsYellowRoomsMap(map)) return false;
            return YellowRoomsUtility.GetResearchers() != null;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            var map = (Map)parms.target;
            var faction = YellowRoomsUtility.GetResearchers();
            if (faction == null) return false;

            float points = parms.points > 0 ? parms.points : StorytellerUtility.DefaultThreatPointsNow(map);

            var pawns = YellowRoomsUtility.SpawnGroupAtEdgeVanilla(faction, map, points);
            if (pawns.Count == 0) return false;

            var lordJob = new LordJob_AssaultColony(faction, canKidnap: true, canTimeoutOrFlee: true, sappers: false, useAvoidGridSmart: true);
            LordMaker.MakeNewLord(faction, lordJob, map, pawns);

            var letter = LetterMaker.MakeLetter("YellowRooms_Letter_ResearcherRaid_Title".Translate(),
                "YellowRooms_Letter_ResearcherRaid_Text".Translate(pawns.Count),
                LetterDefOf.ThreatBig, pawns[0]);
            Find.LetterStack.ReceiveLetter(letter);
            return true;
        }
    }
}