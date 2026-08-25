using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;
using Verse.Grammar;

namespace arsiy.Rooms.Quests
{
    public class QuestNode_Root_RefugeeQuest : QuestNode
    {
        private const int TimeoutTicks = 60000;

        private const float BetrayChance = 0.4f;

        private string signalAccept;

        private string signalReject;

        protected override bool TestRunInt(Slate slate)
        {
            if (Faction.OfPlayer == null)
                return false;
            if (Faction.OfPlayer.def.defName == "YellowRooms_StillLifeFaction")
                return false;
            return Find.AnyPlayerHomeMap != null;
        }

        protected override void RunInt()
        {
            Quest quest = QuestGen.quest;
            Slate slate = QuestGen.slate;

            if (!slate.TryGet<Map>("map", out var map))
                map = QuestGen_Get.GetMap();

            Pawn pawn = GeneratePawn(map);
            bool willBetray = Rand.Chance(BetrayChance);
            slate.Set("pawn", pawn);
            QuestGen.AddToGeneratedPawns(pawn);

            AddQuestNameAndDescriptionRules(pawn);

            quest.Delay(TimeoutTicks, delegate
            {
                QuestGen_End.End(quest, QuestEndOutcome.Fail);
            }, isQuestTimeout: true);

            signalAccept = QuestGenUtility.HardcodedSignalWithQuestID("Accept");
            signalReject = QuestGenUtility.HardcodedSignalWithQuestID("Reject");

            quest.Signal(signalAccept, delegate
            {
                quest.SetFaction(Gen.YieldSingle(pawn), Faction.OfPlayer);
                quest.PawnsArrive(Gen.YieldSingle(pawn), null, map.Parent);
                if (willBetray)
                    AddBetrayalTimer(pawn);
                QuestGen_End.End(quest, QuestEndOutcome.Success);
            });
            quest.Signal(signalReject, delegate
            {
                QuestGen_End.End(quest, QuestEndOutcome.Fail);
            });

            AddPawnReward(quest, pawn);
            SendLetter(quest, pawn);
        }

        private static Pawn GeneratePawn(Map map)
        {
            var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                PawnKindDefOf.Colonist, Faction.OfPlayer,
                PawnGenerationContext.NonPlayer, map.Tile));
            pawn.SetFactionDirect(null);
            if (!pawn.IsWorldPawn())
                Find.WorldPawns.PassToWorld(pawn);
            return pawn;
        }

        private static void AddQuestNameAndDescriptionRules(Pawn pawn)
        {
            var nameRules = new List<Rule>
            {
                new Rule_String("questName", "RefugeeRoomsQuestName".Translate(pawn.Named("PAWN")))
            };
            var descRules = new List<Rule>
            {
                new Rule_String("questDescription", "RefugeeRoomsQuestDesc".Translate(pawn.Named("PAWN")))
            };
            QuestGen.AddQuestNameRules(nameRules);
            QuestGen.AddQuestDescriptionRules(descRules);
        }

        private static void AddBetrayalTimer(Pawn pawn)
        {
            var betrayalDef = DefDatabase<HediffDef>.GetNamedSilentFail("YellowRooms_BetrayalTimer");
            if (betrayalDef == null)
                return;
            var part = new QuestPart_AddHediff
            {
                inSignal = QuestGen.slate.Get<string>("inSignal"),
                hediffDef = betrayalDef
            };
            part.pawns.Add(pawn);
            QuestGen.quest.AddPart(part);
        }

        private static void AddPawnReward(Quest quest, Pawn pawn)
        {
            var choice = new QuestPart_Choice();
            var inner = new QuestPart_Choice.Choice();
            inner.rewards.Add(new Reward_Pawn { pawn = pawn, detailsHidden = false });
            choice.choices.Add(inner);
            quest.AddPart(choice);
        }

        private void SendLetter(Quest quest, Pawn pawn)
        {
            var title = "RefugeeRoomsTitle".Translate(pawn.Named("PAWN")).AdjustedFor(pawn);
            var letterText = "RefugeeRoomsLetterDesc".Translate(pawn.Named("PAWN")).AdjustedFor(pawn);
            PawnRelationUtility.TryAppendRelationsWithColonistsInfo(ref letterText, ref title, pawn);
            QuestNode_Root_WandererJoin_WalkIn.ApplyBestSkillInfoToLetter(ref letterText, pawn);

            var letter = (ChoiceLetter_AcceptJoiner)LetterMaker.MakeLetter(title, letterText, LetterDefOf.AcceptJoiner);
            letter.signalAccept = signalAccept;
            letter.signalReject = signalReject;
            letter.quest = quest;
            letter.StartTimeout(TimeoutTicks);
            Find.LetterStack.ReceiveLetter(letter);
        }
    }
}
