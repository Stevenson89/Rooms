using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace arsiy.Rooms.Incidents
{
    
    
    
    public class IncidentWorker_StillLifeFromPit : IncidentWorker_YellowRoomsBase
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            var map = (Map)parms.target;
            return YellowRoomsUtility.GetStillLifeFaction() != null
                && YellowRoomsUtility.GetPits(map).Count > 0;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            var map = (Map)parms.target;
            var faction = YellowRoomsUtility.GetStillLifeFaction();
            if (faction == null) return false;

            var pits = YellowRoomsUtility.GetPits(map);
            if (pits.Count == 0) return false;
            var pit = pits.RandomElement();

            
            int count = YellowRoomsUtility.StillLifeCountFor(parms.points * 0.5f);
            var pawns = new List<Pawn>();
            for (int i = 0; i < count; i++)
            {
                var kind = YellowRoomsUtility.RandomStillLifeKind();
                var cell = CellFinder.RandomClosewalkCellNear(pit.Position, map, 3);
                var p = YellowRoomsUtility.SpawnHostilePawn(kind, faction, cell, map);
                if (p != null) pawns.Add(p);
            }
            if (pawns.Count == 0) return false;

            var letter = LetterMaker.MakeLetter("YellowRooms_Letter_StillLifePit_Title".Translate(),
                "YellowRooms_Letter_StillLifePit_Text".Translate(pawns.Count),
                LetterDefOf.ThreatBig, pit);
            Find.LetterStack.ReceiveLetter(letter);

            LordMaker.MakeNewLord(faction, new LordJob_AssaultColony(faction, true, true, false, true), map, pawns);
            return true;
        }
    }

    
    
    
    public class IncidentWorker_StillLifeFromCeiling : IncidentWorker_YellowRoomsBase
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            var map = (Map)parms.target;
            return YellowRoomsUtility.GetStillLifeFaction() != null
                && YellowRoomsUtility.GetCeilingHoles(map).Count > 0;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            var map = (Map)parms.target;
            var faction = YellowRoomsUtility.GetStillLifeFaction();
            if (faction == null) return false;

            var holes = YellowRoomsUtility.GetCeilingHoles(map);
            if (holes.Count == 0) return false;
            var hole = holes.RandomElement();

            
            int count = YellowRoomsUtility.StillLifeCountFor(parms.points * 0.3f);
            var pawns = new List<Pawn>();
            for (int i = 0; i < count; i++)
            {
                var kind = YellowRoomsUtility.RandomStillLifeKind();
                var cell = CellFinder.RandomClosewalkCellNear(hole.Position, map, 3);
                var p = YellowRoomsUtility.SpawnHostilePawn(kind, faction, cell, map);
                if (p != null) pawns.Add(p);
            }
            if (pawns.Count == 0) return false;

            var letter = LetterMaker.MakeLetter("YellowRooms_Letter_StillLifeCeiling_Title".Translate(),
                "YellowRooms_Letter_StillLifeCeiling_Text".Translate(pawns.Count),
                LetterDefOf.ThreatBig, hole);
            Find.LetterStack.ReceiveLetter(letter);

            LordMaker.MakeNewLord(faction, new LordJob_AssaultColony(faction, true, true, false, true), map, pawns);
            return true;
        }
    }
}
