using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace arsiy.Rooms.YellowRooms
{
    public class HediffGiver_MoldExposure : HediffGiver
    {
        static HediffDef MoldExposure => DefDatabase<HediffDef>.GetNamed("MoldExposure");
        public float SeverityPerDayRooms;
        public override void OnIntervalPassed(Pawn pawn, Hediff cause)
        {
            if (YellowRoomsUtility.PawnInRooms(pawn) && GasUtility.IsAffectedByExposure(pawn))
            {
                float num = SeverityPerDayRooms * (1f / 300f);
                IEnumerable<BodyPartRecord> affectedBodyParts = GasUtility.GetLungRotAffectedBodyParts(pawn);
                if (!affectedBodyParts.Any())
                    return;
                foreach (BodyPartRecord part in affectedBodyParts)
                {
                    List<Hediff> hediffs = new List<Hediff>();
                    pawn.health.hediffSet.GetHediffs(ref hediffs);
                    Hediff firstHediff = hediffs.Where(h => h.Part == part).FirstOrDefault();
                    if (firstHediff != null)
                        firstHediff.Severity += num;
                    else
                        pawn.health.AddHediff(MoldExposure, part).Severity = num;
                }
            }
        }
    }
}
