using RimWorld;
using RimWorld.Planet;
using Verse;
using arsiy.Rooms.Things;

namespace arsiy.Rooms.Incidents
{
    
    
    
    
    
    
    public class IncidentWorker_SurvivorTradeSettlement : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            if (YellowRoomsUtility.GetSurvivorTraders() == null) return false;
            
            foreach (var wo in Find.WorldObjects.AllWorldObjects)
            {
                if (wo is SurvivorTradeSettlement)
                    return false;
            }
            return SiteMakerUtility.TryFindRandomSitePlanetTile_YellowRooms(out _, 5, 15);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!SiteMakerUtility.TryFindRandomSitePlanetTile_YellowRooms(out var tile, 5, 15))
                return false;

            var faction = YellowRoomsUtility.GetSurvivorTraders();
            if (faction == null) return false;

            var def = DefDatabase<WorldObjectDef>.GetNamedSilentFail("YellowRooms_TradeSettlement");
            if (def == null)
            {
                RoomsLog.Error("[Rooms] SurvivorTradeSettlement: WorldObjectDef not found");
                return false;
            }

            var settlement = (SurvivorTradeSettlement)WorldObjectMaker.MakeWorldObject(def);
            settlement.Tile = tile;
            settlement.SetFaction(faction);
            settlement.Name = "YellowRooms_TradeSettlement_Name".Translate();
            Find.WorldObjects.Add(settlement);
            settlement.StartDespawnCountdown();

            var letter = LetterMaker.MakeLetter(
                "YellowRooms_Letter_TradeSettlement_Title".Translate(),
                "YellowRooms_Letter_TradeSettlement_Text".Translate(
                    SurvivorTradeSettlement.DespawnDays.ToString()),
                LetterDefOf.NeutralEvent, settlement);
            Find.LetterStack.ReceiveLetter(letter);
            return true;
        }
    }
}