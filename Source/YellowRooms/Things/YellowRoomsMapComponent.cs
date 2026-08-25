using System.Collections.Generic;
using System.Linq;
using arsiy.Rooms;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace arsiy.Rooms.Things
{
    public class YellowRoomsMapComponent : MapComponent
    {
        private const int CheckIntervalTicks = 2000;
        private const float RegenChancePerCheck = 0.02f;
        private const float OutletRegenChancePerCheck = 0.004f; 
        private const int PassiveCloneAggroDelay = 60000;
        private const int SpreadSampleSize = 150;
        private const int LampRegenLetterCooldown = 60000; 
        private const int KidnapCheckInterval = 120; 

        private const string BrokenDefName = "YellowRooms_CeilingLampBroken";
        private const string WorkingDefName = "YellowRooms_CeilingLamp";
        private const string OutletBrokenDefName = "YellowRooms_WallOutletBroken";
        private const string OutletWorkingDefName = "YellowRooms_WallOutlet";

        private static readonly IntVec3[] Cardinals =
        {
            new IntVec3(1, 0, 0), new IntVec3(-1, 0, 0),
            new IntVec3(0, 0, 1), new IntVec3(0, 0, -1)
        };

        private TerrainDef _wetCarpetDef;
        private TerrainDef _dryCarpetDef;
        private TerrainDef _ceramicDef;
        private ThingDef _mushroomDef;
        private ThingDef _brokenLampDef;
        private ThingDef _workingLampDef;
        private ThingDef _brokenOutletDef;
        private ThingDef _workingOutletDef;
        private bool _defsResolved;
        private bool _factionsInitialized;

        private Dictionary<Pawn, int> _passiveClones;
        private List<Pawn> _passiveCloneKeys;
        private List<int> _passiveCloneValues;
        private int _lastLampRegenLetterTick = -999999;
        private int _lastOutletRegenLetterTick = -999999;

        private int _nextThreatTick = -1;
        private const int ThreatIntervalMin = 4;
        private const int ThreatIntervalMax = 10;
        private const int FirstThreatMin = 5;
        private const int FirstThreatMax = 10;

        
        
        
        private List<Pawn> _pitSurvivors;

        public YellowRoomsMapComponent(Map map) : base(map)
        {
            _passiveClones = new Dictionary<Pawn, int>();
        }

        public void Init(Map map)
        {
            RoomsLog.Message("[Rooms] YellowRooms map initialized: " + map.Size);
        }

        public static void EnsureLayerReachability()
        {
            try
            {
                var layerDef = DefDatabase<PlanetLayerDef>.GetNamedSilentFail("YellowRooms");
                if (layerDef != null)
                {
                    var layer = Find.WorldGrid.FirstLayerOfDef(layerDef);
                    if (layer != null)
                    {
                        Find.WorldPathGrid.RecalculateLayerPerceivedPathCosts(layer);
                        Find.WorldReachability.ClearCache();
                    }
                }
            }
            catch (System.Exception ex)
            {
                RoomsLog.Warning($"[Rooms] EnsureLayerReachability failed (non-fatal): {ex.Message}");
            }
        }

        public void TrackPassiveClone(Pawn clone)
        {
            if (clone == null || clone.Destroyed) return;
            _passiveClones ??= new Dictionary<Pawn, int>();
            _passiveClones[clone] = PassiveCloneAggroDelay;
        }

        public void NotifyPassiveCloneDamaged(Pawn pawn, DamageInfo dinfo)
        {
            if (pawn == null || pawn.Destroyed) return;
            if (_passiveClones == null || !_passiveClones.ContainsKey(pawn)) return;
            if (dinfo.Instigator == null || dinfo.Instigator.Faction == null || dinfo.Instigator.Faction == pawn.Faction) return;

            RoomsLog.Message($"[Rooms] Passive clone {pawn.LabelShort} took damage from {dinfo.Instigator.LabelShort} and turned hostile");
            _passiveClones.Remove(pawn);
            MakeAggressive(pawn);
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            if (Find.TickManager == null || Find.TickManager.TicksGame <= 0) return;

            if (Find.TickManager.TicksGame % KidnapCheckInterval == 0)
                YellowRoomsUtility.TryForceNearestResearcherKidnap(map);

            if (Find.TickManager.TicksGame % CheckIntervalTicks != 0) return;

            if (!_factionsInitialized)
            {
                YellowRoomsUtility.GetStillLifeFaction();
                YellowRoomsUtility.GetPassiveStillLifeFaction();
                YellowRoomsUtility.GetResearchers();
                YellowRoomsUtility.GetSurvivors();
                _factionsInitialized = true;
            }

            CacheDefsOnce();

            if (Rand.Value < RegenChancePerCheck)
                TryRegenerateLamp();

            if (Rand.Value < OutletRegenChancePerCheck)
                TryRegenerateOutlet();

            TickPassiveClones();

            TrySpreadMushroom();
            TrySpreadCarpet();
            TrySpreadCarpetToCeramic();

            TryScheduleThreat();
        }

        private void TryScheduleThreat()
        {
            if (!YellowRoomsUtility.IsYellowRoomsMap(map))
                return;

            if (_nextThreatTick < 0)
            {
                _nextThreatTick = Find.TickManager.TicksGame + GenDate.TicksPerDay * Rand.RangeInclusive(FirstThreatMin, FirstThreatMax);
                return;
            }

            if (Find.TickManager.TicksGame < _nextThreatTick)
                return;

            var threats = DefDatabase<IncidentDef>.AllDefsListForReading.Where(def =>
            {
                if (def.category != IncidentCategoryDefOf.ThreatBig)
                    return false;
                if (!def.targetTags.Contains(IncidentTargetTagDefOf.Map_PlayerHome))
                    return false;
                return true;
            }).ToList();

            var parms = new IncidentParms
            {
                target = map,
                points = StorytellerUtility.DefaultThreatPointsNow(map)
            };

            var valid = threats.Where(def => def.Worker.CanFireNow(parms)).ToList();
            if (valid.Count > 0)
            {
                var chosen = valid.RandomElementByWeight(def => def.baseChance);
                if (chosen != null)
                {
                    bool success = chosen.Worker.TryExecute(parms);
                    RoomsLog.Message($"[Rooms] Threat timer fired '{chosen.defName}', success={success}");
                }
            }
            else
            {
                RoomsLog.Warning("[Rooms] Threat timer: no valid ThreatBig incidents available, skipping.");
            }

            _nextThreatTick = Find.TickManager.TicksGame + GenDate.TicksPerDay * Rand.RangeInclusive(ThreatIntervalMin, ThreatIntervalMax);
        }

        private void CacheDefsOnce()
        {
            if (_defsResolved) return;
            _wetCarpetDef = DefDatabase<TerrainDef>.GetNamedSilentFail("CarpetYellowWet");
            _dryCarpetDef = DefDatabase<TerrainDef>.GetNamedSilentFail("CarpetYellow");
            _ceramicDef = DefDatabase<TerrainDef>.GetNamedSilentFail("CeramicTile");
            _mushroomDef = ThingDef.Named("YellowRooms_Plant_EdibleMushroom");
            _brokenLampDef = ThingDef.Named(BrokenDefName);
            _workingLampDef = ThingDef.Named(WorkingDefName);
            _brokenOutletDef = ThingDef.Named(OutletBrokenDefName);
            _workingOutletDef = ThingDef.Named(OutletWorkingDefName);
            _defsResolved = true;
        }

        private void TickPassiveClones()
        {
            if (_passiveClones == null || _passiveClones.Count == 0) return;

            var toRemove = new List<Pawn>();
            var keys = new List<Pawn>(_passiveClones.Keys);
            foreach (var pawn in keys)
            {
                if (!_passiveClones.TryGetValue(pawn, out int remaining)) continue;

                if (pawn == null || pawn.Destroyed || !pawn.Spawned)
                {
                    toRemove.Add(pawn);
                    continue;
                }

                if (pawn.guest?.IsPrisoner == true || pawn.guest?.GuestStatus == GuestStatus.Slave)
                {
                    MakeAggressive(pawn);
                    toRemove.Add(pawn);
                    continue;
                }

                remaining -= CheckIntervalTicks;
                if (remaining <= 0)
                {
                    MakeLeave(pawn);
                    toRemove.Add(pawn);
                }
                else
                {
                    _passiveClones[pawn] = remaining;
                }
            }

            foreach (var p in toRemove)
                _passiveClones.Remove(p);
        }

        private void MakeLeave(Pawn pawn)
        {
            if (pawn == null || pawn.Destroyed) return;

            
            var oldLord = pawn.GetLord();
            if (oldLord != null)
            {
                oldLord.Notify_PawnLost(pawn, PawnLostCondition.LeftVoluntarily);
            }

            try
            {
                var lordJob = new LordJob_ExitMapBest(LocomotionUrgency.Jog, canDig: false, canDefendSelf: false);
                LordMaker.MakeNewLord(pawn.Faction, lordJob, map, new List<Pawn> { pawn });
            }
            catch (System.Exception ex)
            {
                RoomsLog.Error($"[Rooms] MakeLeave failed for {pawn}: {ex}");
                return;
            }

            var letter = LetterMaker.MakeLetter("YellowRooms_Letter_EchoFades_Title".Translate(),
                "YellowRooms_Letter_EchoFades_Text".Translate(pawn.LabelShort),
                LetterDefOf.NeutralEvent, pawn);
            Find.LetterStack.ReceiveLetter(letter);
        }

        private void MakeAggressive(Pawn pawn)
        {
            if (pawn == null || pawn.Destroyed) return;

            var faction = arsiy.Rooms.YellowRoomsUtility.GetStillLifeFaction();
            if (faction == null) return;

            pawn.SetFaction(faction);

            var lordJob = new LordJob_AssaultColony(faction, canKidnap: true,
                canTimeoutOrFlee: true, sappers: false, useAvoidGridSmart: true);
            LordMaker.MakeNewLord(faction, lordJob, map, new List<Pawn> { pawn });

            var letter = LetterMaker.MakeLetter("YellowRooms_Letter_EchoHostile_Title".Translate(),
                "YellowRooms_Letter_EchoHostile_Text".Translate(pawn.LabelShort),
                LetterDefOf.ThreatBig, pawn);
            Find.LetterStack.ReceiveLetter(letter);
        }

        private void TryRegenerateLamp()
        {
            if (_brokenLampDef == null || _workingLampDef == null) return;

            var broken = map.listerThings.ThingsOfDef(_brokenLampDef);
            if (broken.Count == 0) return;

            var target = broken[Rand.Range(0, broken.Count)];
            if (target == null || target.Destroyed) return;

            var cell = target.Position;
            var rot = target.Rotation;
            target.DeSpawn();
            GenSpawn.Spawn(_workingLampDef, cell, map, rot, WipeMode.Vanish);

            
            int currentTick = Find.TickManager.TicksGame;
            if (currentTick - _lastLampRegenLetterTick >= LampRegenLetterCooldown)
            {
                _lastLampRegenLetterTick = currentTick;
                var letter = LetterMaker.MakeLetter("LampRegeneratedTitle".Translate(),
                    "LampRegenerated".Translate(), LetterDefOf.PositiveEvent, new TargetInfo(cell, map));
                Find.LetterStack.ReceiveLetter(letter);
            }
        }

        private void TryRegenerateOutlet()
        {
            if (_brokenOutletDef == null || _workingOutletDef == null) return;

            var broken = map.listerThings.ThingsOfDef(_brokenOutletDef);
            if (broken.Count == 0) return;

            var target = broken[Rand.Range(0, broken.Count)];
            if (target == null || target.Destroyed) return;

            var cell = target.Position;
            var rot = target.Rotation;
            target.DeSpawn();
            GenSpawn.Spawn(_workingOutletDef, cell, map, rot, WipeMode.Vanish);

            int currentTick = Find.TickManager.TicksGame;
            if (currentTick - _lastOutletRegenLetterTick >= LampRegenLetterCooldown)
            {
                _lastOutletRegenLetterTick = currentTick;
                var letter = LetterMaker.MakeLetter("OutletRegeneratedTitle".Translate(),
                    "OutletRegenerated".Translate(), LetterDefOf.PositiveEvent, new TargetInfo(cell, map));
                Find.LetterStack.ReceiveLetter(letter);
            }
        }

        private void TrySpreadMushroom()
        {
            if (_wetCarpetDef == null || _mushroomDef == null) return;

            var existing = map.listerThings.ThingsOfDef(_mushroomDef);
            if (existing.Count >= 30) return;

            for (int i = 0; i < SpreadSampleSize; i++)
            {
                var cell = CellFinder.RandomCell(map);
                if (!cell.InBounds(map)) continue;
                if (map.terrainGrid.TerrainAt(cell) != _wetCarpetDef) continue;
                if (!cell.Standable(map)) continue;
                if (cell.GetPlant(map) != null) continue;
                if (cell.GetFirstItem(map) != null) continue;
                if (cell.GetEdifice(map) != null) continue;

                var plant = GenSpawn.Spawn(_mushroomDef, cell, map, Rot4.North, WipeMode.Vanish) as Plant;
                if (plant != null)
                    plant.Growth = Rand.Range(0.1f, 0.6f);
                return;
            }
        }

        private void TrySpreadCarpet()
        {
            if (_wetCarpetDef == null || _dryCarpetDef == null) return;

            for (int i = 0; i < SpreadSampleSize; i++)
            {
                var cell = CellFinder.RandomCell(map);
                if (!cell.InBounds(map)) continue;
                if (map.terrainGrid.TerrainAt(cell) != _wetCarpetDef) continue;

                foreach (var d in Cardinals)
                {
                    var n = cell + d;
                    if (!n.InBounds(map)) continue;
                    if (map.terrainGrid.TerrainAt(n) == _dryCarpetDef)
                    {
                        map.terrainGrid.SetTerrain(n, _wetCarpetDef);
                        return;
                    }
                }
            }
        }

        private void TrySpreadCarpetToCeramic()
        {
            if (_dryCarpetDef == null || _ceramicDef == null) return;

            for (int i = 0; i < SpreadSampleSize; i++)
            {
                var cell = CellFinder.RandomCell(map);
                if (!cell.InBounds(map)) continue;
                if (map.terrainGrid.TerrainAt(cell) != _dryCarpetDef) continue;

                foreach (var d in Cardinals)
                {
                    var n = cell + d;
                    if (!n.InBounds(map)) continue;
                    if (map.terrainGrid.TerrainAt(n) == _ceramicDef)
                    {
                        map.terrainGrid.SetTerrain(n, _dryCarpetDef);
                        return;
                    }
                }
            }
        }

        public int GetNextThreatTick() => _nextThreatTick;

        public void ForceNextThreat() => _nextThreatTick = 0;

        
        
        public void QueuePitSurvivor(Pawn survivor)
        {
            if (survivor == null || survivor.Destroyed) return;
            survivor.DeSpawn();
            _pitSurvivors ??= new List<Pawn>();
            if (!_pitSurvivors.Contains(survivor))
                _pitSurvivors.Add(survivor);
        }

        
        
        public List<Pawn> TakePitSurvivors()
        {
            if (_pitSurvivors == null || _pitSurvivors.Count == 0) return new List<Pawn>();
            var result = _pitSurvivors.Where(s => s != null && !s.Destroyed).ToList();
            _pitSurvivors.Clear();
            return result;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref _passiveClones, "passiveClones", LookMode.Reference, LookMode.Value, ref _passiveCloneKeys, ref _passiveCloneValues);
            Scribe_Values.Look(ref _nextThreatTick, "nextThreatTick", -1);
            Scribe_Values.Look(ref _lastLampRegenLetterTick, "lastLampRegenLetterTick", -999999);
            Scribe_Values.Look(ref _lastOutletRegenLetterTick, "lastOutletRegenLetterTick", -999999);
            Scribe_Collections.Look(ref _pitSurvivors, "pitSurvivors", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                _passiveClones ??= new Dictionary<Pawn, int>();
                _passiveClones.RemoveAll(kvp => kvp.Key == null || kvp.Key.Destroyed);
                _pitSurvivors?.RemoveAll(s => s == null || s.Destroyed);
            }
        }
    }
}
