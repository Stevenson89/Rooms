using RimWorld;
using RimWorld.Planet;
using Verse;
using arsiy.Rooms.Things;

namespace arsiy.Rooms.Incidents
{
    public class IncidentWorker_WorldSite_AbandonedBase : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return base.CanFireNowSub(parms)
                && SiteMakerUtility.TryFindRandomSitePlanetTile_YellowRooms(out _, 5, 15);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!SiteMakerUtility.TryFindRandomSitePlanetTile_YellowRooms(out var tile, 5, 15))
                return false;

            var sitePartDef = DefDatabase<SitePartDef>.GetNamedSilentFail("YellowRooms_AbandonedResearcherBase");
            if (sitePartDef == null)
            {
                RoomsLog.Error("[Rooms] AbandonedResearcherBase: SitePartDef not found");
                return false;
            }

            var worldObjectDef = DefDatabase<WorldObjectDef>.GetNamedSilentFail("YellowRooms_AbandonedResearcherBaseSite");
            if (worldObjectDef == null)
            {
                RoomsLog.Error("[Rooms] AbandonedResearcherBase: WorldObjectDef not found");
                return false;
            }

            var site = SiteMaker.MakeSite(sitePartDef, tile, null,
                ifHostileThenMustRemainHostile: false, threatPoints: 0f,
                worldObjectDef: worldObjectDef);
            if (site == null) return false;
            Find.WorldObjects.Add(site);

            var letter = LetterMaker.MakeLetter(def.label,
                "YellowRooms_Letter_AbandonedResearcher_Desc".Translate() + "YellowRooms_Letter_WorldSite_Text".Translate(),
                LetterDefOf.NeutralEvent, site);
            Find.LetterStack.ReceiveLetter(letter);
            return true;
        }
    }

    public class IncidentWorker_WorldSite_ResearcherBase : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return base.CanFireNowSub(parms)
                && SiteMakerUtility.TryFindRandomSitePlanetTile_YellowRooms(out _, 5, 15);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!SiteMakerUtility.TryFindRandomSitePlanetTile_YellowRooms(out var tile, 5, 15))
                return false;

            var sitePartDef = DefDatabase<SitePartDef>.GetNamedSilentFail("YellowRooms_ResearcherBase");
            if (sitePartDef == null)
            {
                RoomsLog.Error("[Rooms] ResearcherBase: SitePartDef not found");
                return false;
            }

            var worldObjectDef = DefDatabase<WorldObjectDef>.GetNamedSilentFail("YellowRooms_ResearcherBaseSite");
            if (worldObjectDef == null)
            {
                RoomsLog.Error("[Rooms] ResearcherBase: WorldObjectDef not found");
                return false;
            }

            var site = SiteMaker.MakeSite(sitePartDef, tile, null,
                ifHostileThenMustRemainHostile: false, threatPoints: 0f,
                worldObjectDef: worldObjectDef);
            if (site == null) return false;
            Find.WorldObjects.Add(site);

            var letter = LetterMaker.MakeLetter(def.label,
                "YellowRooms_Letter_ActiveResearcher_Desc".Translate() + "YellowRooms_Letter_WorldSite_Text".Translate(),
                LetterDefOf.NeutralEvent, site);
            Find.LetterStack.ReceiveLetter(letter);
            return true;
        }
    }

    public class IncidentWorker_WorldSite_SurfaceResearcherBase : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return base.CanFireNowSub(parms) && YellowRoomsUtility.GetResearchers() != null;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            var faction = YellowRoomsUtility.GetResearchers();
            if (faction == null) return false;

            
            if (!TileFinder.TryFindNewSiteTile(out var tile, 3, 12))
                return false;

            
            var surfaceLayerDef = DefDatabase<PlanetLayerDef>.GetNamedSilentFail("Surface");
            if (tile.Layer?.Def != surfaceLayerDef)
                return false;

            var sitePartDef = DefDatabase<SitePartDef>.GetNamedSilentFail("YellowRooms_SurfaceResearcherBase");
            if (sitePartDef == null)
            {
                RoomsLog.Error("[Rooms] SurfaceResearcherBase: SitePartDef not found");
                return false;
            }

            var worldObjectDef = DefDatabase<WorldObjectDef>.GetNamedSilentFail("YellowRooms_SurfaceResearcherBase");
            if (worldObjectDef == null)
            {
                RoomsLog.Error("[Rooms] SurfaceResearcherBase: WorldObjectDef not found");
                return false;
            }

            var site = SiteMaker.MakeSite(sitePartDef, tile, faction,
                ifHostileThenMustRemainHostile: true, threatPoints: 0f,
                worldObjectDef: worldObjectDef);
            if (site == null) return false;
            Find.WorldObjects.Add(site);

            var letter = LetterMaker.MakeLetter("YellowRooms_Letter_SurfaceBase_Title".Translate(),
                "YellowRooms_Letter_SurfaceBase_Text".Translate(),
                LetterDefOf.NeutralEvent, site);
            Find.LetterStack.ReceiveLetter(letter);
            return true;
        }
    }
}