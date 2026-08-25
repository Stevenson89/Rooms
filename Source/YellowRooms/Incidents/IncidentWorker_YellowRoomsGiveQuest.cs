using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace arsiy.Rooms.Incidents
{
    public class IncidentWorker_YellowRoomsGiveQuest : IncidentWorker_GiveQuest
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (Faction.OfPlayer != null && Faction.OfPlayer.def.defName == "YellowRooms_StillLifeFaction")
                return false;
            return base.CanFireNowSub(parms);
        }

        protected override void GiveQuest(IncidentParms parms, QuestScriptDef questDef)
        {
            var slate = new Slate();
            slate.Set("points", parms.points);
            if (parms.target is Map map)
                slate.Set("map", map);
            var quest = QuestUtility.GenerateQuestAndMakeAvailable(questDef, slate);
            if (!quest.hidden && questDef.sendAvailableLetter)
                QuestUtility.SendLetterQuestAvailable(quest);
        }
    }
}
