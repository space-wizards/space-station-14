using Content.Client.Utility;
using Content.Shared.GameObjects.Components.PDA;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client.GameObjects.Components.PDA
{
    public class PDABoundUserInterface : BoundUserInterface
    {

        private PDAMenu _menu;
        private ClientUserInterfaceComponent Owner;
        public PDABoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
            Owner = owner;
        }

        protected override void Open()
        {
            base.Open();
            _menu = new PDAMenu(this);
            _menu.OpenToLeft();
            _menu.OnClose += Close;
            _menu.FlashLightToggleButton.OnToggled += args =>
            {
                SendMessage(new PDAToggleFlashlightMessage());

            };

            _menu.EjectIDButton.OnPressed += args =>
            {
                SendMessage(new PDAEjectIDMessage());
            };
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            DebugTools.Assert((state is PDAUpdateUserInterfaceState));
            var cstate = (PDAUpdateUserInterfaceState) state;
            switch (state)
            {

            }
            _menu.FlashLightToggleButton.Pressed = cstate.FlashlightEnabled;
            _menu.PDAOwnerLabel.SetMarkup(Loc.GetString("Owner: [color=white]{0}[/color]",cstate.PDAOwnerInfo.ActualOwnerName));

            if (cstate.PDAOwnerInfo.JobTitle == null || cstate.PDAOwnerInfo.IDOwner == null)
            {
                _menu.IDInfoLabel.SetMarkup(Loc.GetString("ID:"));
            }
            else
            {
                _menu.IDInfoLabel.SetMarkup(Loc.GetString("ID: [color=white]{0}[/color], [color=yellow]{1}[/color]",
                    cstate.PDAOwnerInfo.IDOwner,
                    cstate.PDAOwnerInfo.JobTitle));
            }

            _menu.EjectIDButton.Visible = cstate.PDAOwnerInfo.IDOwner != null;

        }



        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _menu?.Dispose();

        }


        private class PDAMenu : SS14Window
        {
            protected override Vector2? CustomSize => (512, 256);

            private PDABoundUserInterface _owner { get; }

            public Button FlashLightToggleButton { get; }
            public Button EjectIDButton { get; }

            public TabContainer MasterTabContainer;

            public RichTextLabel PDAOwnerLabel { get; }
            public PanelContainer IDInfoContainer { get; }
            public RichTextLabel IDInfoLabel { get; }


            private ScrollContainer _uplinkShopScrollContainer;

            public PDAMenu(PDABoundUserInterface owner = null)
            {
                //CustomMinimumSize = (380, 128);

                _owner = owner;
                Title = Loc.GetString("PDA");

                #region MAIN_MENU_TAB


                //Main menu
                PDAOwnerLabel = new RichTextLabel
                {

                };

                IDInfoLabel = new RichTextLabel()
                {
                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                };

                EjectIDButton = new Button
                {
                    Text = Loc.GetString("Eject ID"),
                    SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                    SizeFlagsVertical = SizeFlags.ShrinkCenter

                };

                var innerHBoxContainer = new HBoxContainer
                {
                    Children =
                    {
                        IDInfoLabel,
                        EjectIDButton
                    }
                };

                IDInfoContainer = new PanelContainer
                {
                    SizeFlagsHorizontal = SizeFlags.Fill,
                    Children =
                    {
                        innerHBoxContainer,
                    }
                };

                FlashLightToggleButton = new Button
                {
                    Text = Loc.GetString("Toggle Flashlight"),
                    ToggleMode = true,
                };

                var mainMenuTabContainer = new VBoxContainer
                {
                    SizeFlagsVertical = SizeFlags.FillExpand,
                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                    CustomMinimumSize = (50,50),

                    Children =
                    {
                        PDAOwnerLabel,
                        IDInfoContainer,
                        FlashLightToggleButton
                    }
                };
                #endregion

                #region UPLINK_TAB
                 //Messaging Tab

                 var uplinkStoreHeader = new Label
                 {
                    Align = Label.AlignMode.Center,
                    Text = Loc.GetString("Uplink Listings"),
                 };

                 _uplinkShopScrollContainer = new ScrollContainer();
                var innerVboxContainer = new VBoxContainer
                 {
                    Children =
                    {
                        uplinkStoreHeader,
                        _uplinkShopScrollContainer
                    }
                 };

                 var uplinkTabContainer = new VBoxContainer
                 {
                     Children =
                     {
                         innerVboxContainer
                     }
                 };
                #endregion

                MasterTabContainer = new TabContainer
                {

                    Children =
                    {
                        mainMenuTabContainer,
                        uplinkTabContainer
                    }
                };

                MasterTabContainer.SetTabTitle(0,Loc.GetString("Main Menu"));
                MasterTabContainer.SetTabTitle(1,Loc.GetString("Uplink -DEBUG-"));
                Contents.AddChild(MasterTabContainer);
            }

            public void AddListingGUI(UplinkStoreListing listing)
            {
                var itemLabel = new Label
                {
                    Text = listing.Item.Name

                };
                var priceLabel = new Label
                {
                    Text = listing.Price.ToString(),
                };
                var hbox = new HBoxContainer
                {
                    Children =
                    {
                        itemLabel,
                        priceLabel
                    }
                };
                var button = new PDAUplinkItemButton
                {
                    Children =
                    {
                        hbox
                    }
                };
                _uplinkShopScrollContainer.AddChild(button);
            }
            private sealed class PDAUplinkItemButton : ContainerButton
            {

            }
        }
    }



}
