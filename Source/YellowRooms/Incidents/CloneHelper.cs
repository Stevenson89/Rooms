using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace arsiy.Rooms.Incidents
{
    
    
    
    
    public static class CloneHelper
    {
        private static HediffDef _stillLifeDef;
        private static bool _lookedUp;

        private static HediffDef StillLifeDef
        {
            get
            {
                if (!_lookedUp)
                {
                    _stillLifeDef = DefDatabase<HediffDef>.GetNamedSilentFail("YellowRooms_StillLife");
                    _lookedUp = true;
                }
                return _stillLifeDef;
            }
        }

        
        
        public static IEnumerable<Pawn> AllColonyPawns()
        {
            if (PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists != null)
            {
                foreach (var p in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists)
                    yield return p;
            }
            if (PawnsFinder.AllMaps_PrisonersOfColony != null)
            {
                foreach (var p in PawnsFinder.AllMaps_PrisonersOfColony)
                    yield return p;
            }
        }

        
        
        
        
        
        public static Pawn PickCloneSourcePawn()
        {
            var colonists = AllColonyPawns().Where(p => p != null && !p.Dead).ToList();
            if (colonists.Count == 0) return null;

            
            if (Rand.Value < 0.5f)
            {
                var candidates = new List<Pawn>();
                foreach (var col in colonists)
                {
                    if (col.relations == null) continue;
                    foreach (var rel in col.relations.RelatedPawns)
                    {
                        if (rel == null || rel.Dead || rel == col) continue;
                        
                        if (!rel.RaceProps.Humanlike) continue;
                        
                        if (rel.Faction != null && rel.Faction.def?.defName == "YellowRooms_StillLifeFaction")
                            continue;
                        candidates.Add(rel);
                    }
                }
                if (candidates.Count > 0)
                    return candidates.RandomElement();
            }

            return colonists.RandomElement();
        }

        
        
        
        
        
        
        public static Pawn MakeHostileClone(Pawn original, Faction faction)
        {
            if (original == null || faction == null) return null;
            var kind = PawnKindDef.Named("YellowRooms_StillLife_Melee");
            var req = new PawnGenerationRequest(
                kind, null, PawnGenerationContext.NonPlayer,
                tile: original.Tile, forceGenerateNewPawn: true,
                fixedGender: original.gender, colonistRelationChanceFactor: 0f,
                forceNoGear: true, forceNoIdeo: true, forceNoBackstory: true);
            var clone = PawnGenerator.GeneratePawn(req);
            if (clone == null)
            {
                RoomsLog.Warning("[Rooms] MakeHostileClone: PawnGenerator returned null");
                return null;
            }

            CopyIdentityFrom(original, clone);
            clone.SetFaction(faction);
            ApplyStillLife(clone);
            return clone;
        }

        
        
        
        
        
        
        public static Pawn MakeSurvivorClone(Pawn original)
        {
            if (original == null) return null;
            var faction = YellowRoomsUtility.GetSurvivors();
            if (faction == null)
            {
                RoomsLog.Warning("[Rooms] MakeSurvivorClone: Survivors faction is null");
                return null;
            }
            var kind = PawnKindDef.Named("YellowRooms_Survivor_Crazed");
            if (kind == null)
            {
                RoomsLog.Warning("[Rooms] MakeSurvivorClone: PawnKindDef YellowRooms_Survivor_Crazed not found");
                return null;
            }
            var req = new PawnGenerationRequest(
                kind, null, PawnGenerationContext.NonPlayer,
                tile: original.Tile, forceGenerateNewPawn: true,
                fixedGender: original.gender, colonistRelationChanceFactor: 0f,
                forceNoGear: true, forceNoIdeo: true, forceNoBackstory: true);
            var clone = PawnGenerator.GeneratePawn(req);
            if (clone == null)
            {
                RoomsLog.Warning("[Rooms] MakeSurvivorClone: PawnGenerator.GeneratePawn returned null");
                return null;
            }

            CopyIdentityFrom(original, clone, copyWeapons: false);
            clone.SetFaction(faction);
            return clone;
        }

        
        
        
        
        private static void CopyIdentityFrom(Pawn original, Pawn clone, bool copyWeapons = true)
        {
            
            
            clone.Name = original.Name is NameTriple nt
                ? new NameTriple(nt.First, nt.Nick, nt.Last)
                : original.Name;
            clone.ageTracker?.DebugSetAge(original.ageTracker?.AgeBiologicalTicks ?? clone.ageTracker.AgeBiologicalTicks);

            
            if (clone.story?.traits != null && original.story?.traits != null)
            {
                clone.story.traits.allTraits.Clear();
                foreach (var t in original.story.traits.allTraits)
                    clone.story.traits.GainTrait(new Trait(t.def, t.Degree, t.ScenForced), suppressConflicts: true);
            }


            if (clone.story != null && original.story != null)
            {
                clone.story.Childhood = original.story.Childhood;
                clone.story.Adulthood = original.story.Adulthood;
            }


            if (clone.story != null && original.story != null)
            {
                clone.story.skinColorOverride = original.story.skinColorOverride;
            }

            
            if (ModsConfig.BiotechActive && clone.genes != null && original.genes != null)
            {
                
                clone.genes.SetXenotype(original.genes.Xenotype);
                clone.genes.ClearXenogenes();
                foreach (var g in original.genes.Xenogenes)
                    clone.genes.AddGene(g.def, xenogene: true);
            }

            
            if (ModsConfig.IdeologyActive && clone.ideo != null && original.ideo?.Ideo != null)
                clone.ideo.SetIdeo(original.ideo.Ideo);

            
            if (clone.skills != null && original.skills != null)
            {
                foreach (SkillDef sd in DefDatabase<SkillDef>.AllDefs)
                {
                    var src = original.skills.GetSkill(sd);
                    var dst = clone.skills.GetSkill(sd);
                    if (src != null && dst != null)
                    {
                        dst.Level = src.Level;
                        dst.passion = src.passion;
                    }
                }
            }

            
            CopyGearFrom(original, clone, copyWeapons);
        }

        
        
        
        private static void CopyGearFrom(Pawn original, Pawn clone, bool copyWeapons = true)
        {
            clone.apparel?.DestroyAll();
            if (clone.equipment?.Primary != null)
                clone.equipment.Remove(clone.equipment.Primary);
            clone.inventory?.innerContainer?.Clear();

            if (original.apparel != null && clone.apparel != null)
            {
                foreach (var ap in original.apparel.WornApparel)
                {
                    var copy = (Apparel)ThingMaker.MakeThing(ap.def, ap.Stuff);
                    CopyThingState(ap, copy);
                    clone.apparel.Wear(copy, dropReplacedApparel: false);
                }
            }

            if (copyWeapons)
            {
                if (original.equipment?.Primary != null)
                {
                    var w = original.equipment.Primary;
                    var copy = ThingMaker.MakeThing(w.def, w.Stuff);
                    CopyThingState(w, copy);
                    clone.equipment?.AddEquipment((ThingWithComps)copy);
                }

                if (original.inventory?.innerContainer != null && clone.inventory?.innerContainer != null)
                {
                    foreach (var thing in original.inventory.innerContainer)
                    {
                        var copy = ThingMaker.MakeThing(thing.def, thing.Stuff);
                        copy.stackCount = thing.stackCount;
                        CopyThingState(thing, copy);
                        clone.inventory.innerContainer.TryAdd(copy);
                    }
                }
            }
        }

        
        private static void CopyThingState(Thing source, Thing made)
        {
            var srcQ = source.TryGetComp<CompQuality>();
            var dstQ = made.TryGetComp<CompQuality>();
            if (srcQ != null && dstQ != null)
                dstQ.SetQuality(srcQ.Quality, ArtGenerationContext.Outsider);
            if (source.def.useHitPoints && made.def.useHitPoints)
                made.HitPoints = source.HitPoints;
        }


        
        public static Pawn MakePassiveClone(Pawn original)
        {
            if (original == null)
            {
                RoomsLog.Warning("[Rooms] MakePassiveClone: original is null");
                return null;
            }
            var faction = YellowRoomsUtility.GetPassiveStillLifeFaction();
            if (faction == null)
            {
                RoomsLog.Warning("[Rooms] MakePassiveClone: PassiveStillLifeFaction is null");
                return null;
            }
            var kind = PawnKindDef.Named("YellowRooms_StillLife_Drifter");
            if (kind == null)
            {
                RoomsLog.Warning("[Rooms] MakePassiveClone: PawnKindDef YellowRooms_StillLife_Drifter not found");
                return null;
            }
            var req = new PawnGenerationRequest(
                kind, null, PawnGenerationContext.NonPlayer,
                tile: original.Tile, forceGenerateNewPawn: true,
                fixedGender: original.gender, colonistRelationChanceFactor: 0f,
                forceNoGear: true, forceNoIdeo: true, forceNoBackstory: true);
            var clone = PawnGenerator.GeneratePawn(req);
            if (clone == null)
            {
                RoomsLog.Warning("[Rooms] MakePassiveClone: PawnGenerator.GeneratePawn returned null");
                return null;
            }

            CopyIdentityFrom(original, clone, copyWeapons: false);
            clone.SetFaction(faction);
            ApplyStillLife(clone);
            RoomsLog.Message($"[Rooms] MakePassiveClone: Successfully created clone {clone.LabelShort} from {original.LabelShort}");
            return clone;
        }

        
        
        public static void ApplyStillLife(Pawn pawn)
        {
            var def = StillLifeDef;
            if (def == null || pawn?.health?.hediffSet == null) return;
            if (!pawn.health.hediffSet.HasHediff(def))
            {
                var h = HediffMaker.MakeHediff(def, pawn);
                pawn.health.AddHediff(h);
            }
            ApplyMissingParts(pawn);
            ApplyRandomModifications(pawn);
        }

        
        public static void ApplyRandomModifications(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null) return;

            
            if (Rand.Value < 0.3f)
            {
                var fusedDef = DefDatabase<HediffDef>.GetNamedSilentFail("YellowRooms_FusedArms");
                if (fusedDef != null)
                {
                    var shoulders = pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined)
                        .Where(p => p.def.defName == "Shoulder").ToList();
                    if (shoulders.Count > 0)
                    {
                        var h = HediffMaker.MakeHediff(fusedDef, pawn, shoulders.RandomElement());
                        pawn.health.AddHediff(h);
                    }
                }
            }

            
            if (Rand.Value < 0.3f)
            {
                var eyesDef = DefDatabase<HediffDef>.GetNamedSilentFail("YellowRooms_ExtraEyes");
                if (eyesDef != null)
                {
                    var eyes = pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined)
                        .Where(p => p.def.defName == "Eye").ToList();
                    if (eyes.Count > 0)
                    {
                        var h = HediffMaker.MakeHediff(eyesDef, pawn, eyes.RandomElement());
                        pawn.health.AddHediff(h);
                    }
                }
            }

            
            if (Rand.Value < 0.3f)
            {
                var faceDef = DefDatabase<HediffDef>.GetNamedSilentFail("YellowRooms_DisfiguredFace");
                if (faceDef != null && !pawn.health.hediffSet.HasHediff(faceDef))
                {
                    var head = pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined)
                        .FirstOrDefault(p => p.def.defName == "Head");
                    if (head != null)
                    {
                        var h = HediffMaker.MakeHediff(faceDef, pawn, head);
                        pawn.health.AddHediff(h);
                    }
                }
            }

            
            if (Rand.Value < 0.2f)
            {
                var def = DefDatabase<HediffDef>.GetNamedSilentFail("YellowRooms_InorganicFusion");
                if (def != null && !pawn.health.hediffSet.HasHediff(def))
                {
                    var torso = pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined)
                        .FirstOrDefault(p => p.def.defName == "Torso");
                    if (torso != null)
                    {
                        var h = HediffMaker.MakeHediff(def, pawn, torso);
                        pawn.health.AddHediff(h);
                    }
                }
            }

            
            if (Rand.Value < 0.25f)
            {
                var def = DefDatabase<HediffDef>.GetNamedSilentFail("YellowRooms_DistortedVocalCords");
                if (def != null && !pawn.health.hediffSet.HasHediff(def))
                {
                    var head = pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined)
                        .FirstOrDefault(p => p.def.defName == "Head");
                    if (head != null)
                    {
                        var h = HediffMaker.MakeHediff(def, pawn, head);
                        pawn.health.AddHediff(h);
                    }
                }
            }

            
            if (Rand.Value < 0.2f)
            {
                var def = DefDatabase<HediffDef>.GetNamedSilentFail("YellowRooms_ElongatedLimbs");
                if (def != null)
                {
                    var shoulders = pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined)
                        .Where(p => p.def.defName == "Shoulder").ToList();
                    if (shoulders.Count > 0)
                    {
                        var h = HediffMaker.MakeHediff(def, pawn, shoulders.RandomElement());
                        pawn.health.AddHediff(h);
                    }
                }
            }

            
            if (Rand.Value < 0.25f)
            {
                var def = DefDatabase<HediffDef>.GetNamedSilentFail("YellowRooms_Runner");
                if (def != null && !pawn.health.hediffSet.HasHediff(def))
                {
                    var h = HediffMaker.MakeHediff(def, pawn);
                    pawn.health.AddHediff(h);
                }
            }

            
            if (Rand.Value < 0.15f)
            {
                var def = DefDatabase<HediffDef>.GetNamedSilentFail("YellowRooms_Hypertrophy");
                if (def != null && !pawn.health.hediffSet.HasHediff(def))
                {
                    var h = HediffMaker.MakeHediff(def, pawn);
                    pawn.health.AddHediff(h);
                }
            }

            
            if (Rand.Value < 0.15f)
            {
                var def = DefDatabase<HediffDef>.GetNamedSilentFail("YellowRooms_Faceless");
                if (def != null && !pawn.health.hediffSet.HasHediff(def))
                {
                    var head = pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined)
                        .FirstOrDefault(p => p.def.defName == "Head");
                    if (head != null)
                    {
                        var h = HediffMaker.MakeHediff(def, pawn, head);
                        pawn.health.AddHediff(h);
                        ApplyFacePartsRemoval(pawn);
                    }
                }
            }
        }

        
        private static void ApplyFacePartsRemoval(Pawn pawn)
        {
            string[] facePartNames = { "Eye", "Jaw", "Nose" };
            var toRemove = new List<BodyPartRecord>();
            foreach (var p in pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined))
            {
                if (facePartNames.Contains(p.def.defName))
                    toRemove.Add(p);
            }
            foreach (var part in toRemove)
            {
                var miss = HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, pawn, part);
                pawn.health.AddHediff(miss);
            }
        }

        
        public static void ApplyMissingParts(Pawn pawn)
        {
            string[] partNames = { "Finger", "Toe", "Ear", "Nose" };
            var available = new List<BodyPartRecord>();
            foreach (var p in pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined))
            {
                if (partNames.Contains(p.def.defName))
                    available.Add(p);
            }
            if (available.Count == 0) return;

            int count = Rand.RangeInclusive(1, 2);
            for (int i = 0; i < count && available.Count > 0; i++)
            {
                var part = available.RandomElement();
                available.Remove(part);
                var miss = HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, pawn, part);
                pawn.health.AddHediff(miss);
            }
        }
    }
}
