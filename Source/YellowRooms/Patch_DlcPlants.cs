using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace arsiy.Rooms
{
    
    
    
    
    
    
    
    
    
    
    
    
    [StaticConstructorOnStartup]
    public static class Patch_DlcPlants
    {
        private const string SowTag = "YellowRoomsCarpet";

        private static readonly DLCPlantPatch[] Patches =
        {
            new DLCPlantPatch
            {
                DefName = "Plant_Psilocap",
                PackageId = "Ludeon.RimWorld.Odyssey",
                ResearchPrereq = "YellowRooms_AdvancedMushroomFarming",
            },
            new DLCPlantPatch
            {
                DefName = "Boomshroom",
                PackageId = "Ludeon.RimWorld.Odyssey",
                ResearchPrereq = "YellowRooms_AdvancedMushroomFarming",
            },
            new DLCPlantPatch
            {
                DefName = "Plant_Timbershroom",
                PackageId = "Ludeon.RimWorld.Odyssey",
                ResearchPrereq = "YellowRooms_TimbershroomFarming",
            },
        };

        static Patch_DlcPlants()
        {
            try
            {
                Apply();
            }
            catch (Exception ex)
            {
                RoomsLog.Error($"[Rooms] Patch_DlcPlants failed: {ex}");
            }
        }

        private static bool IsDlcLoaded(string packageId)
        {
            return LoadedModManager.RunningModsListForReading?.Any(
                m => string.Equals(m.PackageId, packageId, StringComparison.OrdinalIgnoreCase)) ?? false;
        }

        private static void Apply()
        {
            foreach (var patch in Patches)
            {
                
                if (!IsDlcLoaded(patch.PackageId))
                {
                    RoomsLog.Message($"[Rooms] DLC '{patch.PackageId}' not loaded — skipping patch for {patch.DefName}.");
                    continue;
                }

                var plantDef = DefDatabase<ThingDef>.GetNamedSilentFail(patch.DefName);
                if (plantDef == null || plantDef.plant == null)
                {
                    RoomsLog.Warning($"[Rooms] Def '{patch.DefName}' not found or has no plant data — skipping DLC patch.");
                    continue;
                }

                
                if (plantDef.plant.sowTags == null)
                    plantDef.plant.sowTags = new List<string>();
                if (!plantDef.plant.sowTags.Contains(SowTag))
                    plantDef.plant.sowTags.Add(SowTag);

                
                if (plantDef.plant.sowResearchPrerequisites == null)
                    plantDef.plant.sowResearchPrerequisites = new List<ResearchProjectDef>();
                else
                    plantDef.plant.sowResearchPrerequisites.Clear();

                var researchDef = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(patch.ResearchPrereq);
                if (researchDef != null)
                    plantDef.plant.sowResearchPrerequisites.Add(researchDef);
                else
                    RoomsLog.Warning($"[Rooms] Research def '{patch.ResearchPrereq}' not found — {patch.DefName} will have no research prereq on mushroom farm.");

                RoomsLog.Message($"[Rooms] Patched {patch.DefName}: added sowTag '{SowTag}', research prereq '{patch.ResearchPrereq}'.");
            }
        }

        private struct DLCPlantPatch
        {
            public string DefName;
            public string PackageId;
            public string ResearchPrereq;
        }
    }
}
