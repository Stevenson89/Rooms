using RimWorld;
using Verse;

namespace arsiy.Rooms.Incidents
{
    
    
    
    public class IncidentWorker_YellowWeather : IncidentWorker_YellowRoomsBase
    {
        private WeatherDef ResolveWeather(IncidentDef def)
        {
            switch (def.defName)
            {
                case "YellowRooms_Fog": return WeatherDefOf.Fog;
                default: return WeatherDefOf.Clear;
            }
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            var map = (Map)parms.target;
            var weather = ResolveWeather(def);
            if (weather == null) return false;

            map.weatherManager.TransitionTo(weather);

            var letter = LetterMaker.MakeLetter(weather.label.CapitalizeFirst(),
                "YellowRooms_Letter_WeatherShift".Translate(),
                LetterDefOf.NeutralEvent);
            Find.LetterStack.ReceiveLetter(letter);
            return true;
        }
    }

    
    
    
    
    public class IncidentWorker_YellowGameCondition : IncidentWorker_YellowRoomsBase
    {
        private GameConditionDef ResolveCondition(IncidentDef def)
        {
            switch (def.defName)
            {
                case "YellowRooms_ColdSnap": return DefDatabase<GameConditionDef>.GetNamed("YellowRooms_ColdSnapIndoor");
                case "YellowRooms_HeatWave": return DefDatabase<GameConditionDef>.GetNamed("YellowRooms_HeatWaveIndoor");
                default: return null;
            }
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            var map = (Map)parms.target;
            var condDef = ResolveCondition(def);
            if (condDef == null) return false;

            var condition = GameConditionMaker.MakeCondition(condDef, Rand.RangeInclusive(30000, 90000));
            map.gameConditionManager.RegisterCondition(condition);

            var letter = LetterMaker.MakeLetter(condition.LabelCap,
                "YellowRooms_Letter_ConditionSettles".Translate(),
                LetterDefOf.NeutralEvent);
            Find.LetterStack.ReceiveLetter(letter);
            return true;
        }
    }

    
    
    
    public class GameCondition_HeatWaveIndoor : GameCondition_HeatWave
    {
        public override float TemperatureOffset()
        {
            
            return 25f;
        }

        public override bool AllowEnjoyableOutsideNow(Map map)
        {
            return false;
        }

        public override float SkyTargetLerpFactor(Map map)
        {
            return 0f;
        }
    }

    
    
    
    public class GameCondition_ColdSnapIndoor : GameCondition_ColdSnap
    {
        public override float TemperatureOffset()
        {
            
            return -25f;
        }

        public override bool AllowEnjoyableOutsideNow(Map map)
        {
            return false;
        }

        public override float SkyTargetLerpFactor(Map map)
        {
            return 0f;
        }
    }
}
