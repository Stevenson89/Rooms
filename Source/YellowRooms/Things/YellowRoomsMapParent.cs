using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace arsiy.Rooms.Things
{
    public class YellowRoomsMapParent : MapParent
    {
        
        
        
        
        public override MapGeneratorDef MapGeneratorDef
        {
            get
            {
                
                var biome = Biome;
                string genName = "YellowRooms";
                if (biome != null)
                {
                    if (biome.defName == "YellowRooms_PoolRooms")
                        genName = "YellowRooms_PoolRooms";
                    else if (biome.defName == "YellowRooms_Parking")
                        genName = "YellowRooms_Parking";
                }
                return DefDatabase<MapGeneratorDef>.GetNamedSilentFail(genName);
            }
        }

        public override IEnumerable<IncidentTargetTagDef> IncidentTargetTags()
        {
            foreach (IncidentTargetTagDef item in base.IncidentTargetTags())
            {
                yield return item;
            }
            if (Faction == Faction.OfPlayer)
            {
                yield return IncidentTargetTagDefOf.Map_PlayerHome;
            }
            else
            {
                yield return IncidentTargetTagDefOf.Map_Misc;
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
                yield return g;

            
            
            
            
            
            
            
            
            if (HasMap && Faction == Faction.OfPlayer)
            {
                var cmd = MakeAbandonCommand();
                if (cmd != null)
                    yield return cmd;
            }
        }

        
        
        
        
        
        private Command_Action MakeAbandonCommand()
        {
            
            if (Map != null && Map.mapPawns.AnyColonistSpawned)
                return null;

            return new Command_Action
            {
                defaultLabel = "CommandAbandonLocation".Translate(),
                defaultDesc = "CommandAbandonLocationDesc".Translate(),
                icon = TexCommandForAbandon(),
                action = AbandonLocation,
            };
        }

        
        private void AbandonLocation()
        {
            try
            {
                if (HasMap && Map != null)
                {
                    Current.Game.DeinitAndRemoveMap(Map, notifyPlayer: false);
                }
                Find.WorldObjects.Remove(this);
            }
            catch (Exception ex)
            {
                RoomsLog.Error($"[Rooms] AbandonLocation failed: {ex}");
            }
        }

        private static Texture2D TexCommandForAbandon()
        {
            return ContentFinder<Texture2D>.Get("UI/Commands/Abandon", reportFailure: false)
                ?? TexCommand.ForbidOff;
        }

        public Map GenerateYellowRoomsMap(int? mapSizeOverride = null)
        {
            var genDef = MapGeneratorDef;
            if (genDef == null)
            {
                RoomsLog.Error("[Rooms] YellowRooms MapGeneratorDef not found");
                return null;
            }

            var size = mapSizeOverride ?? RoomsSettings.MapSize;
            var mapSize = new IntVec3(size, 1, size);
            try
            {
                var map = MapGenerator.GenerateMap(mapSize, this, genDef, null, null);
                return map;
            }
            catch (Exception ex)
            {
                RoomsLog.Error($"[Rooms] Failed to generate YellowRooms map: {ex}");
                return null;
            }
        }
    }

    public class YellowRoomsFoundStructure : YellowRoomsMapParent
    {
        public override bool CanReformFoggedEnemies => true;

        public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject)
        {
            alsoRemoveWorldObject = false;
            if (Map != null && Map.mapPawns.AnyPawnBlockingMapRemoval)
            {
                return false;
            }
            if (TransporterUtility.IncomingTransporterPreventingMapRemoval(Map))
            {
                return false;
            }
            alsoRemoveWorldObject = true;
            return true;
        }
    }

    public class ResearcherBaseSite : Site
    {
        public override AcceptanceReport CanBeSettled => false;
    }

    public class AbandonedResearcherBaseSite : Site
    {
        public override AcceptanceReport CanBeSettled => false;
    }

    
    
    
    
    public class SurfaceResearcherBase : Site
    {
        public override MapGeneratorDef MapGeneratorDef => DefDatabase<MapGeneratorDef>.GetNamedSilentFail("YellowRooms_SurfaceBase");

        public override bool ShowRelatedQuests => Faction != Faction.OfPlayer;

        public override AcceptanceReport CanBeSettled => false;
    }
}