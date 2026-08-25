using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace arsiy.Rooms.Things
{
    
    
    
    
    
    
    
    
    
    public class Dialog_YellowRoomsPortal : Window
    {
        private enum Tab
        {
            Pawns,
            Items
        }

        private const float TitleRectHeight = 35f;
        private const float BottomAreaHeight = 55f;
        private readonly Vector2 BottomButtonSize = new Vector2(160f, 40f);

        private IYellowRoomsPortalCargo portal;
        private List<TransferableOneWay> transferables;
        private TransferableOneWayWidget pawnsTransfer;
        private TransferableOneWayWidget itemsTransfer;
        private Tab tab;

        private static List<TabRecord> tabsList = new List<TabRecord>();

        public override Vector2 InitialSize => new Vector2(1024f, UI.screenHeight);

        protected override float Margin => 0f;

        public Dialog_YellowRoomsPortal(IYellowRoomsPortalCargo portal)
        {
            this.portal = portal;
            forcePause = true;
            absorbInputAroundWindow = true;
        }

        public override void PostOpen()
        {
            base.PostOpen();
            CalculateAndRecacheTransferables();
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect rect = new Rect(0f, 0f, inRect.width, 35f);
            using (new TextBlock(GameFont.Medium, TextAnchor.MiddleCenter))
            {
                Widgets.Label(rect, portal.EnterLabel);
            }
            tabsList.Clear();
            tabsList.Add(new TabRecord("PawnsTab".Translate(), delegate
            {
                tab = Tab.Pawns;
            }, tab == Tab.Pawns));
            tabsList.Add(new TabRecord("ItemsTab".Translate(), delegate
            {
                tab = Tab.Items;
            }, tab == Tab.Items));
            inRect.yMin += 67f;
            Widgets.DrawMenuSection(inRect);
            TabDrawer.DrawTabs(inRect, tabsList);
            inRect = inRect.ContractedBy(17f);
            Widgets.BeginGroup(inRect);
            Rect rect2 = inRect.AtZero();
            DoBottomButtons(rect2);
            Rect inRect2 = rect2;
            inRect2.yMax -= 76f;
            bool anythingChanged = false;
            switch (tab)
            {
                case Tab.Pawns:
                    pawnsTransfer.OnGUI(inRect2, out anythingChanged);
                    break;
                case Tab.Items:
                    itemsTransfer.OnGUI(inRect2, out anythingChanged);
                    break;
            }
            Widgets.EndGroup();
        }

        private void DoBottomButtons(Rect rect)
        {
            if (Widgets.ButtonText(new Rect(rect.width / 2f - BottomButtonSize.x / 2f, rect.height - 55f - 17f, BottomButtonSize.x, BottomButtonSize.y), "ResetButton".Translate()))
            {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                CalculateAndRecacheTransferables();
            }
            if (Widgets.ButtonText(new Rect(0f, rect.height - 55f - 17f, BottomButtonSize.x, BottomButtonSize.y), "CancelButton".Translate()))
            {
                Close();
            }
            if (Widgets.ButtonText(new Rect(rect.width - BottomButtonSize.x, rect.height - 55f - 17f, BottomButtonSize.x, BottomButtonSize.y), "AcceptButton".Translate()) && TryAccept())
            {
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
                Close(doCloseSound: false);
            }
        }

        private bool TryAccept()
        {
            List<Pawn> pawnsFromTransferables = TransferableUtility.GetPawnsFromTransferables(transferables);
            return portal.AcceptTransferSelection(transferables, pawnsFromTransferables, null);
        }

        private void CalculateAndRecacheTransferables()
        {
            transferables = new List<TransferableOneWay>();
            if (portal.Loading)
            {
                foreach (TransferableOneWay item in portal.LeftToLoad)
                {
                    if (item != null)
                        transferables.Add(item);
                }
            }
            AddPawnsToTransferables();
            AddItemsToTransferables();
            foreach (Thing item2 in ThingsBeingHauledToPortal())
            {
                AddToTransferables(item2);
            }
            pawnsTransfer = new TransferableOneWayWidget(null, null, null, "TransferMapPortalColonyThingCountTip".Translate(), drawMass: true, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, includePawnsMassInMassUsage: true, () => float.MaxValue, 0f, ignoreSpawnedCorpseGearAndInventoryMass: false, portal.SourceMap.Tile, drawMarketValue: false, drawEquippedWeapon: true);
            CaravanUIUtility.AddPawnsSections(pawnsTransfer, transferables);
            itemsTransfer = new TransferableOneWayWidget(transferables.Where((TransferableOneWay x) => x.ThingDef.category != ThingCategory.Pawn), null, null, "TransferMapPortalColonyThingCountTip".Translate(), drawMass: true, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, includePawnsMassInMassUsage: true, () => float.MaxValue, 0f, ignoreSpawnedCorpseGearAndInventoryMass: false, portal.SourceMap.Tile);
        }

        private void AddToTransferables(Thing t)
        {
            if (!portal.Loading || !portal.LeftToLoad.Any((TransferableOneWay trans) => trans != null && trans.things.Contains(t)))
            {
                TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatching(t, transferables, TransferAsOneMode.PodsOrCaravanPacking);
                if (transferableOneWay == null)
                {
                    transferableOneWay = new TransferableOneWay();
                    transferables.Add(transferableOneWay);
                }
                if (transferableOneWay.things.Contains(t))
                {
                    RoomsLog.Error("Tried to add the same thing twice to TransferableOneWay: " + t);
                }
                else
                {
                    transferableOneWay.things.Add(t);
                }
            }
        }

        private void AddPawnsToTransferables()
        {
            foreach (Pawn item in CaravanFormingUtility.AllSendablePawns(portal.SourceMap, allowEvenIfDowned: true, allowEvenIfInMentalState: false, allowEvenIfPrisonerNotSecure: false, allowCapturableDownedPawns: false, allowLodgers: true))
            {
                AddToTransferables(item);
            }
        }

        private void AddItemsToTransferables()
        {
            bool isPocketMap = portal.SourceMap.IsPocketMap;
            foreach (Thing item in CaravanFormingUtility.AllReachableColonyItems(portal.SourceMap, isPocketMap, isPocketMap))
            {
                AddToTransferables(item);
            }
        }

        private IEnumerable<Thing> ThingsBeingHauledToPortal()
        {
            IReadOnlyList<Pawn> pawns = portal.SourceMap.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i].CurJobDef == JobDefOf_Rooms.CarryToYellowRoomsPortal
                    && pawns[i].jobs.curDriver is JobDriver_CarryToYellowRoomsPortal drv
                    && drv.Cargo == portal
                    && pawns[i].carryTracker.CarriedThing != null)
                {
                    yield return pawns[i].carryTracker.CarriedThing;
                }
            }
        }

        public override void OnAcceptKeyPressed()
        {
            if (TryAccept())
            {
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
                Close(doCloseSound: false);
            }
        }
    }
}
