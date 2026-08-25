using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;
using arsiy.Rooms.Things;

namespace arsiy.Rooms.Incidents
{
    public abstract class IncidentWorker_CaravanYellowBase : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return YellowRoomsUtility.IsYellowRoomsCaravan(parms.target as Caravan);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            var caravan = parms.target as Caravan;
            if (!YellowRoomsUtility.IsYellowRoomsCaravan(caravan)) return false;
            return ExecuteForCaravan(caravan, parms);
        }

        protected abstract bool ExecuteForCaravan(Caravan caravan, IncidentParms parms);
    }

    public abstract class IncidentWorker_CaravanAmbushBase : IncidentWorker_Ambush_EnemyFaction
    {
        protected abstract Faction GetAmbushFaction();

        protected virtual Faction GetPawnSourceFaction(Faction ambushFaction)
        {
            return ambushFaction;
        }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!(parms.target is Caravan caravan))
            {
                return false;
            }
            if (!YellowRoomsUtility.IsYellowRoomsCaravan(caravan))
            {
                return false;
            }
            var ambushFaction = GetAmbushFaction();
            if (ambushFaction == null)
            {
                return false;
            }
            if (GetPawnSourceFaction(ambushFaction) == null)
            {
                return false;
            }
            return CaravanIncidentUtility.CanFireIncidentWhichWantsToGenerateMapAt(parms.target.Tile);
        }

        protected override List<Pawn> GeneratePawns(IncidentParms parms)
        {
            var ambushFaction = GetAmbushFaction();
            parms.faction = ambushFaction;
            var sourceFaction = GetPawnSourceFaction(ambushFaction) ?? ambushFaction;
            PawnGroupMakerParms pawnGroupMakerParms = IncidentParmsUtility.GetDefaultPawnGroupMakerParms(PawnGroupKindDefOf.Combat, parms);
            pawnGroupMakerParms.faction = sourceFaction;
            pawnGroupMakerParms.generateFightersOnly = true;
            pawnGroupMakerParms.dontUseSingleUseRocketLaunchers = true;
            var pawns = PawnGroupMakerUtility.GeneratePawns(pawnGroupMakerParms).ToList();
            if (sourceFaction != ambushFaction)
            {
                var stillLifeDef = DefDatabase<HediffDef>.GetNamedSilentFail("YellowRooms_StillLife");
                foreach (var pawn in pawns)
                {
                    pawn.SetFaction(ambushFaction);
                    if (stillLifeDef != null && !pawn.health.hediffSet.HasHediff(stillLifeDef))
                    {
                        pawn.health.AddHediff(stillLifeDef);
                        CloneHelper.ApplyMissingParts(pawn);
                        CloneHelper.ApplyRandomModifications(pawn);
                    }
                }
            }
            return pawns;
        }

        protected override string GetLetterText(Pawn anyPawn, IncidentParms parms)
        {
            Caravan caravan = parms.target as Caravan;
            return def.letterText.Formatted(
                caravan != null ? caravan.Name : "yourCaravan".TranslateSimple(),
                parms.faction.def.pawnsPlural,
                parms.faction.NameColored).Resolve().CapitalizeFirst();
        }
    }

    public class IncidentWorker_CaravanStillLife : IncidentWorker_CaravanAmbushBase
    {
        protected override Faction GetAmbushFaction()
        {
            return YellowRoomsUtility.GetStillLifeFaction();
        }

        protected override Faction GetPawnSourceFaction(Faction ambushFaction)
        {
            return YellowRoomsUtility.TryGetRandomHumanlikeFaction(out var source, false) ? source : null;
        }
    }

    public class IncidentWorker_CaravanResearchers : IncidentWorker_CaravanAmbushBase
    {
        protected override Faction GetAmbushFaction()
        {
            return YellowRoomsUtility.GetResearchers();
        }
    }

    public class IncidentWorker_CaravanFoundBuilding : IncidentWorker_CaravanYellowBase
    {
        private const int SearchRadius = 18;

        private const string SiteDefName = "YellowRooms_FoundStructureSite";
        private const string BatteryDefName = "Battery";

        private static readonly string[] CommonBuildingDefNames =
        {
            "ChemfuelPoweredGenerator", "ElectricSmithy", "ElectricStove",
            "ElectricTailoringBench", "TableMachining", "TubeTelevision"
        };

        private static readonly string[] RareBuildingDefNames =
        {
            "HiTechResearchBench", "MultiAnalyzer", "FabricationBench"
        };

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            return CaravanIncidentUtility.CanFireIncidentWhichWantsToGenerateMapAt(parms.target.Tile);
        }

        protected override bool ExecuteForCaravan(Caravan caravan, IncidentParms parms)
        {
            var tile = caravan.Tile;
            var site = (YellowRoomsMapParent)WorldObjectMaker.MakeWorldObject(
                DefDatabase<WorldObjectDef>.GetNamed(SiteDefName));
            site.Tile = tile;
            Find.WorldObjects.Add(site);

            var map = site.GenerateYellowRoomsMap(RoomsSettings.CampMapSize);
            if (map == null) { Find.WorldObjects.Remove(site); return false; }

            site.GetComponent<TimedDetectionRaids>()?.StartDetectionCountdown(240000);

            CaravanEnterMapUtility.Enter(caravan, map, CaravanEnterMode.Edge, CaravanDropInventoryMode.DropInstantly, draftColonists: false);

            var buildingPool = Rand.Chance(0.8f) ? CommonBuildingDefNames : RareBuildingDefNames;
            var buildingDef = ThingDef.Named(buildingPool.RandomElement());
            if (buildingDef == null) return true;

            RoomsLog.Message($"[Rooms] FoundStructure: spawned '{buildingDef.defName}' ({buildingDef.label}) with discharged battery at tile {tile}");

            var stuff = buildingDef.MadeFromStuff ? GenStuff.RandomStuffFor(buildingDef) : null;
            var building = ThingMaker.MakeThing(buildingDef, stuff);
            var buildingCell = FindSuitableCellNearCenter(map, building);
            GenSpawn.Spawn(building, buildingCell, map, Rot4.North);

            var batteryDef = ThingDef.Named(BatteryDefName);
            if (batteryDef != null)
            {
                var battery = ThingMaker.MakeThing(batteryDef);
                battery.TryGetComp<CompPowerBattery>()?.SetStoredEnergyPct(0f);
                var rect = GenAdj.OccupiedRect(buildingCell, Rot4.North, buildingDef.size);
                var batteryCell = GenAdj.CellsAdjacentCardinal(buildingCell, Rot4.North, buildingDef.size)
                    .FirstOrDefault(c => !rect.Contains(c) && c.InBounds(map) && c.Standable(map) && !c.Fogged(map) && !map.thingGrid.ThingsAt(c).Any());
                if (!batteryCell.IsValid)
                {
                    foreach (var c in GenRadial.RadialCellsAround(buildingCell, 2, useCenter: false))
                    {
                        if (!c.InBounds(map) || !c.Standable(map) || c.Fogged(map) || map.thingGrid.ThingsAt(c).Any())
                            continue;
                        batteryCell = c;
                        break;
                    }
                }
                if (batteryCell.IsValid)
                {
                    GenSpawn.Spawn(battery, batteryCell, map, Rot4.North);
                }
                else
                {
                    RoomsLog.Warning("[Rooms] FoundStructure: could not find a free cell for the battery near the structure");
                }
            }

            var letter = LetterMaker.MakeLetter("YellowRooms_Letter_FoundStructure_Title".Translate(),
                "YellowRooms_Letter_FoundStructure_Text".Translate(buildingDef.label),
                LetterDefOf.PositiveEvent, new TargetInfo(buildingCell, map));
            Find.LetterStack.ReceiveLetter(letter);
            return true;
        }

        private static IntVec3 FindSuitableCellNearCenter(Map map, Thing building)
        {
            var center = map.Center;
            for (int offset = 0; offset <= SearchRadius; offset++)
            {
                int radius = 5 + offset;
                if (radius <= SearchRadius && TryFindFreeSpot(map, building, center, radius, out var cell))
                    return cell;
                radius = 5 - offset;
                if (radius >= 0 && TryFindFreeSpot(map, building, center, radius, out cell))
                    return cell;
            }
            return CellFinder.RandomClosewalkCellNear(center, map, SearchRadius);
        }

        private static bool TryFindFreeSpot(Map map, Thing building, IntVec3 center, int radius, out IntVec3 cell)
        {
            foreach (var c in GenRadial.RadialCellsAround(center, radius, useCenter: true))
            {
                if (!c.InBounds(map)) continue;
                var rect = GenAdj.OccupiedRect(c, Rot4.North, building.def.size);
                if (!rect.Cells.All(x => x.InBounds(map) && x.Standable(map) && !x.Fogged(map) && !map.thingGrid.ThingsAt(x).Any()))
                    continue;
                cell = c;
                return true;
            }
            cell = IntVec3.Invalid;
            return false;
        }
    }

    public class IncidentWorker_CaravanTrader : IncidentWorker_CaravanYellowBase
    {
        private const int MapSize = 100;

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            if (!CaravanIncidentUtility.CanFireIncidentWhichWantsToGenerateMapAt(parms.target.Tile)) return false;
            return TryFindSurvivorTraders(out _);
        }

        protected override bool ExecuteForCaravan(Caravan caravan, IncidentParms parms)
        {
            if (!TryFindSurvivorTraders(out var faction)) return false;

            var pawns = GenerateCaravanPawns(faction);
            if (!pawns.Any())
            {
                RoomsLog.Error("IncidentWorker_CaravanTrader could not generate any pawns.");
                return false;
            }

            var metCaravan = CaravanMaker.MakeCaravan(pawns, faction, PlanetTile.Invalid, addToWorldPawnsIfNotAlready: false);
            CameraJumper.TryJumpAndSelect(caravan);

            var diaNode = new DiaNode("CaravanMeeting".Translate(
                caravan.Name, faction.NameColored,
                PawnUtility.PawnKindsToLineList(metCaravan.PawnsListForReading.Select(p => p.kindDef), "  - "))
                .Resolve().CapitalizeFirst());

            var negotiator = BestCaravanPawnUtility.FindBestNegotiator(caravan, faction, metCaravan.TraderKind);

            if (metCaravan.CanTradeNow)
            {
                var tradeOption = new DiaOption("CaravanMeeting_Trade".Translate());
                tradeOption.action = delegate
                {
                    Find.WindowStack.Add(new Dialog_Trade(negotiator, metCaravan));
                    PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter_Send(
                        metCaravan.Goods.OfType<Pawn>(),
                        "LetterRelatedPawnsTradingWithOtherCaravan".Translate(Faction.OfPlayer.def.pawnsPlural),
                        LetterDefOf.NeutralEvent);
                };
                if (negotiator == null)
                {
                    tradeOption.Disable("CaravanMeeting_TradeIncapable".Translate());
                }
                diaNode.options.Add(tradeOption);
            }

            var attackOption = new DiaOption("CaravanMeeting_Attack".Translate());
            attackOption.action = delegate
            {
                LongEventHandler.QueueLongEvent(delegate
                {
                    var pawn = caravan.PawnsListForReading[0];
                    var hostileFaction = YellowRoomsUtility.GetSurvivors() ?? faction;
                    foreach (var metPawn in metCaravan.PawnsListForReading)
                    {
                        metPawn.SetFaction(hostileFaction);
                    }
                    metCaravan.SetFaction(hostileFaction);
                    var map = CaravanIncidentUtility.GetOrGenerateMapForIncident(caravan,
                        new IntVec3(MapSize, 1, MapSize), WorldObjectDefOf.AttackedNonPlayerCaravan);
                    map.Parent.SetFaction(hostileFaction);
                    MultipleCaravansCellFinder.FindStartingCellsFor2Groups(map, out var playerSpot, out var enemySpot);
                    CaravanEnterMapUtility.Enter(caravan, map,
                        p => CellFinder.RandomClosewalkCellNear(playerSpot, map, 12),
                        CaravanDropInventoryMode.DoNotDrop, draftColonists: true);
                    var metPawns = metCaravan.PawnsListForReading.ToList();
                    CaravanEnterMapUtility.Enter(metCaravan, map,
                        p => CellFinder.RandomClosewalkCellNear(enemySpot, map, 12));
                    LordMaker.MakeNewLord(hostileFaction,
                        new LordJob_DefendAttackedTraderCaravan(metPawns[0].Position), map, metPawns);
                    Find.TickManager.Notify_GeneratedPotentiallyHostileMap();
                    CameraJumper.TryJumpAndSelect(pawn);
                    PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter_Send(metPawns,
                        "LetterRelatedPawnsGroupGeneric".Translate(Faction.OfPlayer.def.pawnsPlural),
                        LetterDefOf.NeutralEvent, informEvenIfSeenBefore: true);
                }, "GeneratingMapForNewEncounter", doAsynchronously: false, null);
            };
            attackOption.resolveTree = true;
            diaNode.options.Add(attackOption);

            var leaveOption = new DiaOption("CaravanMeeting_MoveOn".Translate());
            leaveOption.action = delegate
            {
                RemoveAllPawnsAndPassToWorld(metCaravan);
            };
            leaveOption.resolveTree = true;
            diaNode.options.Add(leaveOption);

            var title = "CaravanMeetingTitle".Translate(caravan.Label);
            Find.WindowStack.Add(new Dialog_NodeTreeWithFactionInfo(diaNode, faction, delayInteractivity: true, radioMode: false, title));
            Find.Archive.Add(new ArchivedDialog(diaNode.text, title, faction));
            return true;
        }

        private static bool TryFindSurvivorTraders(out Faction faction)
        {
            faction = YellowRoomsUtility.GetSurvivorTraders();
            return faction != null
                && !faction.HostileTo(Faction.OfPlayer)
                && faction.def.humanlikeFaction
                && !faction.def.caravanTraderKinds.NullOrEmpty()
                && !faction.def.pawnGroupMakers.NullOrEmpty();
        }

        private static List<Pawn> GenerateCaravanPawns(Faction faction)
        {
            return PawnGroupMakerUtility.GeneratePawns(new PawnGroupMakerParms
            {
                groupKind = PawnGroupKindDefOf.Trader,
                faction = faction,
                points = TraderCaravanUtility.GenerateGuardPoints(),
                dontUseSingleUseRocketLaunchers = true
            }).ToList();
        }

        private static void RemoveAllPawnsAndPassToWorld(Caravan caravan)
        {
            var list = caravan.PawnsListForReading;
            for (int i = 0; i < list.Count; i++)
            {
                Find.WorldPawns.PassToWorld(list[i]);
            }
            caravan.RemoveAllPawns();
        }
    }
}
