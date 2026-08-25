using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace arsiy.Rooms
{
    
    
    
    
    
    
    [StaticConstructorOnStartup]
    public static class Patch_RecruitBlocker
    {
        private const string StillLifeHediff = "YellowRooms_StillLife";
        private static HediffDef _stillLifeDef;
        private static bool _lookedUp;

        private static HediffDef StillLifeDef
        {
            get
            {
                if (!_lookedUp)
                {
                    _stillLifeDef = DefDatabase<HediffDef>.GetNamedSilentFail(StillLifeHediff);
                    _lookedUp = true;
                }
                return _stillLifeDef;
            }
        }

        public static void Apply(Harmony harmony)
        {
            
            var methods = AccessTools.GetDeclaredMethods(typeof(InteractionWorker_RecruitAttempt));
            bool any = false;
            foreach (var m in methods)
            {
                if (m.Name != "DoRecruit") continue;
                var prms = m.GetParameters();
                if (prms.Length >= 2 && prms[1].ParameterType == typeof(Pawn))
                {
                    harmony.Patch(m, prefix: new HarmonyMethod(typeof(Patch_RecruitBlocker), nameof(DoRecruit_Prefix)));
                    any = true;
                }
            }
            if (!any)
                RoomsLog.Warning("[Rooms] InteractionWorker_RecruitAttempt.DoRecruit not found; still-life recruit block disabled.");
        }

        
        public static bool DoRecruit_Prefix(Pawn recruitee)
        {
            if (recruitee?.health?.hediffSet == null) return true;
            var def = StillLifeDef;
            if (def != null && recruitee.health.hediffSet.HasHediff(def))
            {
                Messages.Message("YellowRooms_CannotRecruitStillLife".Translate(),
                    recruitee, MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }
            return true;
        }
    }
}
