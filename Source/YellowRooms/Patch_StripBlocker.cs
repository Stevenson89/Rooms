using HarmonyLib;
using RimWorld;
using Verse;

namespace arsiy.Rooms
{
    [StaticConstructorOnStartup]
    public static class Patch_StripBlocker
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
            var method = AccessTools.Method(typeof(StrippableUtility), nameof(StrippableUtility.CanBeStrippedByColony));
            if (method != null)
            {
                harmony.Patch(method, prefix: new HarmonyMethod(typeof(Patch_StripBlocker), nameof(Prefix)));
            }
            else
            {
                RoomsLog.Warning("[Rooms] StrippableUtility.CanBeStrippedByColony not found; strip blocker disabled.");
            }
        }

        public static bool Prefix(Thing th, ref bool __result)
        {
            var def = StillLifeDef;
            if (def == null) return true;

            Pawn pawn = null;
            if (th is Pawn p)
                pawn = p;
            else if (th is Corpse corpse)
                pawn = corpse.InnerPawn;

            if (pawn != null && pawn.health?.hediffSet?.HasHediff(def) == true)
            {
                __result = false;
                return false;
            }

            return true;
        }
    }
}
