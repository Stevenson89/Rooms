using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Noise;

namespace arsiy.Rooms.Incidents
{
    public class IncidentWorker_StillLife : IncidentWorker_YellowRoomsBase
    {
        protected bool Hungry => false;
        protected bool Soulless => true;
        protected LetterDef LetterDef => LetterDefOf.NeutralEvent;
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map target = (Map)parms.target;
            IntVec3 result;
            if (!EntryCellHelper.TryFindEntryCell(target, out result)) return false;
            Pawn source = CloneHelper.PickCloneSourcePawn();
            if (source == null)
                return false;
            Faction faction = YellowRoomsUtility.GetStillLifeFaction();
            Pawn pawn = CloneHelper.MakeHostileClone(source, faction);
            if (pawn == null)
                return false;
            if (pawn.health?.hediffSet != null)
            {
                if (pawn.health.hediffSet.HasHediff(CloneHelper.SoullessDef) && !Soulless)
                {
                    pawn.health.RemoveHediff(pawn.health.hediffSet.GetFirstHediffOfDef(CloneHelper.SoullessDef));
                }
                if (Hungry)
                {
                    pawn.health.AddHediff(CloneHelper.HungryDef);
                }
            }
            Rot4 rot = Rot4.FromAngleFlat((target.Center - result).AngleFlat);
            GenSpawn.Spawn(pawn, result, target, rot);
            if (RoomsSettings.AnnounceIncidents)
            {
                SendIncidentLetter((TaggedString)def.letterLabel, (TaggedString)def.letterText, LetterDef, parms, (LookTargets)pawn, def);
            }
            return true;
        }
    }
}
