using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using arsiy.Rooms.Things;

namespace arsiy.Rooms.Incidents
{
    
    
    
    
    public class IncidentWorker_HostileClone : IncidentWorker_YellowRoomsBase
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            return CloneHelper.AllColonyPawns().Any() && YellowRoomsUtility.GetStillLifeFaction() != null;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            var map = (Map)parms.target;
            var faction = YellowRoomsUtility.GetStillLifeFaction();
            if (faction == null) return false;

            
            var original = CloneHelper.PickCloneSourcePawn();
            if (original == null) return false;

            var clone = CloneHelper.MakeHostileClone(original, faction);
            if (clone == null) return false;

            if (!EntryCellHelper.TryFindEntryCell(map, out IntVec3 cell)) return false;
            GenSpawn.Spawn(clone, cell, map, Rot4.Random);

            var letter = LetterMaker.MakeLetter("YellowRooms_Letter_HostileClone_Title".Translate(),
                "YellowRooms_Letter_HostileClone_Text".Translate(original.LabelShort),
                LetterDefOf.ThreatBig, clone);
            Find.LetterStack.ReceiveLetter(letter);

            AssaultHelper.StartAssault(faction, map, new List<Pawn> { clone }, parms.points);
            return true;
        }
    }

    
    
    
    public class IncidentWorker_PassiveClone : IncidentWorker_YellowRoomsBase
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            var colonists = CloneHelper.AllColonyPawns().Where(p => p != null && !p.Dead).Count();
            var faction = YellowRoomsUtility.GetStillLifeFaction();
            RoomsLog.Message($"[Rooms] PassiveClone CanFireNow: colonists={colonists}, stillLifeFaction={faction != null}");
            return colonists > 0 && faction != null;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            var map = (Map)parms.target;
            
            var original = CloneHelper.PickCloneSourcePawn();
            if (original == null)
            {
                RoomsLog.Warning("[Rooms] PassiveClone: PickCloneSourcePawn returned null");
                return false;
            }
            RoomsLog.Message($"[Rooms] PassiveClone: Original pawn = {original.LabelShort}");

            var clone = CloneHelper.MakePassiveClone(original);
            if (clone == null)
            {
                RoomsLog.Warning("[Rooms] PassiveClone: MakePassiveClone returned null");
                return false;
            }
            RoomsLog.Message($"[Rooms] PassiveClone: Created clone = {clone.LabelShort}");

            
            IntVec3 cell;
            if (!EntryCellHelper.TryFindEntryCell(map, out cell))
            {
                cell = map.Center;
                if (!cell.Standable(map))
                    cell = CellFinder.RandomClosewalkCellNear(map.Center, map, 10);
            }
            RoomsLog.Message($"[Rooms] PassiveClone: Spawning at {cell}");

            var spawnedThing = GenSpawn.Spawn(clone, cell, map, Rot4.Random);
            var spawnedClone = spawnedThing as Pawn;
            if (spawnedClone == null || spawnedClone.Destroyed)
            {
                RoomsLog.Warning($"[Rooms] PassiveClone: Failed to spawn clone at {cell} on map {map}");
                return false;
            }
            RoomsLog.Message($"[Rooms] PassiveClone: Successfully spawned {spawnedClone.LabelShort}");

            var letter = LetterMaker.MakeLetter("YellowRooms_Letter_PassiveClone_Title".Translate(),
                "YellowRooms_Letter_PassiveClone_Text".Translate(original.LabelShort),
                LetterDefOf.NeutralEvent, spawnedClone);
            Find.LetterStack.ReceiveLetter(letter);

            
            var defendJob = new LordJob_DefendPoint(spawnedClone.Position, wanderRadius: 10f);
            LordMaker.MakeNewLord(spawnedClone.Faction, defendJob, map, new List<Pawn> { spawnedClone });

            
            var comp = map.GetComponent<YellowRoomsMapComponent>();
            if (comp != null)
                comp.TrackPassiveClone(spawnedClone);
            else
                RoomsLog.Warning("[Rooms] PassiveClone: YellowRoomsMapComponent not found on map!");
            return true;
        }
    }

    
    
    
    
    public class IncidentWorker_RandomPawnStranded : IncidentWorker_YellowRoomsBase
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            return RoomsSettings.RandomPawnStranded;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            var map = (Map)parms.target;
            var kind = PawnKindDef.Named("Colonist");
            if (kind == null) return false;
            var faction = Faction.OfPlayer;

            var req = new PawnGenerationRequest(
                kind, faction, PawnGenerationContext.PlayerStarter,
                tile: map.Tile, forceGenerateNewPawn: true, colonistRelationChanceFactor: 0f);
            var pawn = PawnGenerator.GeneratePawn(req);
            pawn.SetFactionDirect(faction);

            IntVec3 cell;
            if (!EntryCellHelper.TryFindEntryCell(map, out cell))
            {
                cell = CellFinder.RandomClosewalkCellNear(map.Center, map, 30);
            }
            GenSpawn.Spawn(pawn, cell, map, Rot4.Random);

            var letter = LetterMaker.MakeLetter("YellowRooms_Letter_Stranded_Title".Translate(),
                "YellowRooms_Letter_Stranded_Text".Translate(pawn.LabelShort),
                LetterDefOf.NeutralEvent, pawn);
            Find.LetterStack.ReceiveLetter(letter);
            return true;
        }
    }

    
    internal static class EntryCellHelper
    {
        public static bool TryFindEntryCell(Map map, out IntVec3 cell)
        {
            return YellowRoomsUtility.TryFindRaidEdgeCell(map, out cell);
        }
    }

    
    
    
    
    
    
    
    
    internal static class AssaultHelper
    {
        public static void StartAssault(Faction faction, Map map, List<Pawn> pawns, float points)
        {
            _ = points; 
            var lordJob = new LordJob_AssaultColony(
                faction,
                canKidnap: true,
                canTimeoutOrFlee: true,
                sappers: false,
                useAvoidGridSmart: true,
                canSteal: false,
                breachers: false,
                canPickUpOpportunisticWeapons: false);
            LordMaker.MakeNewLord(faction, lordJob, map, pawns);
        }
    }
}
