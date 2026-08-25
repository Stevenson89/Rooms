using RimWorld;
using UnityEngine;
using Verse;
using System.Collections.Generic;

namespace arsiy.Rooms
{
    [StaticConstructorOnStartup]
    public class PawnRenderNodeWorker_BodyTypeVariant : PawnRenderNodeWorker_AttachmentBody
    {
        private static readonly Dictionary<(string bodyType, string direction), Graphic> GraphicCache = new();

        static PawnRenderNodeWorker_BodyTypeVariant()
        {
            string basePath = "Things/Pawn/YellowRooms/InorganicFusion";
            string[] bodyTypes = { "Male", "Female", "Thin", "Hulk", "Fat", "Child" };
            string[] dirs = { "north", "south", "east" };

            foreach (var bt in bodyTypes)
                foreach (var dir in dirs)
                {
                    string path = $"{basePath}_{bt}_{dir}";
                    var graphic = GraphicDatabase.Get<Graphic_Single>(path, ShaderDatabase.Cutout, Vector2.one, Color.white);
                    GraphicCache[(bt, dir)] = graphic;
                }
        }

        protected override Graphic GetGraphic(PawnRenderNode node, PawnDrawParms parms)
        {
            Pawn pawn = node.hediff?.pawn;
            if (pawn?.story?.bodyType == null)
                return BaseContent.BadGraphic;

            string bodySuffix = GetBodyTypeSuffix(pawn.story.bodyType);
            if (bodySuffix == null)
                return BaseContent.BadGraphic;

            string dirSuffix = GetDirectionSuffix(parms.facing);

            if (GraphicCache.TryGetValue((bodySuffix, dirSuffix), out var graphic))
                return graphic;

            if (dirSuffix == "west" && GraphicCache.TryGetValue((bodySuffix, "east"), out graphic))
                return graphic;

            return BaseContent.BadGraphic;
        }

        private static string GetBodyTypeSuffix(BodyTypeDef bodyType)
        {
            if (bodyType == BodyTypeDefOf.Male) return "Male";
            if (bodyType == BodyTypeDefOf.Female) return "Female";
            if (bodyType == BodyTypeDefOf.Thin) return "Thin";
            if (bodyType == BodyTypeDefOf.Hulk) return "Hulk";
            if (bodyType == BodyTypeDefOf.Fat) return "Fat";
            if (bodyType.defName == "Child") return "Child";
            return null;
        }

        private static string GetDirectionSuffix(Rot4 facing)
        {
            if (facing == Rot4.North) return "north";
            if (facing == Rot4.South) return "south";
            if (facing == Rot4.East) return "east";
            if (facing == Rot4.West) return "west";
            return "south";
        }
    }

    public class InorganicFusionExtension : DefModExtension
    {
        public string texBasePath;
    }
}
