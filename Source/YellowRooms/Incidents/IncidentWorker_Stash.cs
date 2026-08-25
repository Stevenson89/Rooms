using RimWorld;
using RimWorld.Planet;
using Verse;
using arsiy.Rooms.GenSteps;

namespace arsiy.Rooms.Incidents
{
    public class IncidentWorker_SupplyStash : IncidentWorker_YellowRoomsBase
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            return SiteMakerUtility.TryFindRandomSitePlanetTile_YellowRooms(out _, 3, 15);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!SiteMakerUtility.TryFindRandomSitePlanetTile_YellowRooms(out var tile, 3, 15))
                return false;

            var sitePartDef = DefDatabase<SitePartDef>.GetNamedSilentFail("YellowRooms_SupplyStash");
            if (sitePartDef == null)
            {
                RoomsLog.Error("[Rooms] YellowRooms_SupplyStash SitePartDef not found!");
                return false;
            }

            var site = SiteMaker.MakeSite(sitePartDef, tile, null,
                ifHostileThenMustRemainHostile: true, threatPoints: 0f);
            if (site == null) return false;

            var stashType = def.defName switch
            {
                "YellowRooms_Stash_Food" => "Food",
                "YellowRooms_Stash_Materials" => "Materials",
                "YellowRooms_Stash_Chemfuel" => "Chemfuel",
                "YellowRooms_Stash_Weapons" => "Weapons",
                _ => "Default"
            };

            var sitePart = site.parts[0];
            sitePart.things = new ThingOwner<Thing>(sitePart, oneStackOnly: false);
            sitePart.things.TryAddRangeOrTransfer(GenStep_SupplyStash.GenerateLoot(stashType), canMergeWithExistingStacks: false);

            Find.WorldObjects.Add(site);

            var descKey = def.defName switch
            {
                "YellowRooms_Stash_Food" => "YellowRooms_Letter_Stash_Food",
                "YellowRooms_Stash_Materials" => "YellowRooms_Letter_Stash_Materials",
                "YellowRooms_Stash_Chemfuel" => "YellowRooms_Letter_Stash_Chemfuel",
                "YellowRooms_Stash_Weapons" => "YellowRooms_Letter_Stash_Weapons",
                _ => "YellowRooms_Letter_Stash_Default",
            };

            var letter = LetterMaker.MakeLetter("YellowRooms_Letter_SupplyStash_Title".Translate(),
                descKey.Translate() + "YellowRooms_Letter_Stash_Send".Translate(),
                LetterDefOf.PositiveEvent, site);
            Find.LetterStack.ReceiveLetter(letter);
            return true;
        }
    }
}