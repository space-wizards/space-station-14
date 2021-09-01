using Content.Client.Message;
using Content.Shared.PDA;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.PDA
{
    [UsedImplicitly]
    public class PDABoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        private PDAMenu? _menu;

        public PDABoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            SendMessage(new PDARequestUpdateInterfaceMessage());
            _menu = new PDAMenu(this, _prototypeManager);
            _menu.OpenToLeft();
            _menu.OnClose += Close;
            _menu.FlashLightToggleButton.OnToggled += _ =>
            {
                SendMessage(new PDAToggleFlashlightMessage());
            };

            _menu.EjectIDButton.OnPressed += _ =>
            {
                SendMessage(new PDAEjectIDMessage());
            };

            _menu.EjectPenButton.OnPressed += _ =>
            {
                SendMessage(new PDAEjectPenMessage());
            };

            _menu.ActivateUplinkButton.OnPressed += _ =>
            {
                SendMessage(new PDAShowUplinkMessage());
            };

        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (_menu == null)
            {
                return;
            }

            switch (state)
            {
                case PDAUpdateState msg:
                {
                    _menu.FlashLightToggleButton.Pressed = msg.FlashlightEnabled;

                    if (msg.PDAOwnerInfo.ActualOwnerName != null)
                    {
                        _menu.PDAOwnerLabel.SetMarkup(Loc.GetString("comp-pda-ui-owner",
                            ("ActualOwnerName", msg.PDAOwnerInfo.ActualOwnerName)));
                    }


                    if (msg.PDAOwnerInfo.IdOwner != null || msg.PDAOwnerInfo.JobTitle != null)
                    {
                        _menu.IDInfoLabel.SetMarkup(Loc.GetString("comp-pda-ui",
                            ("Owner",msg.PDAOwnerInfo.IdOwner ?? "Unknown"),
                            ("JobTitle",msg.PDAOwnerInfo.JobTitle ?? "Unassigned")));
                    }
                    else
                    {
                        _menu.IDInfoLabel.SetMarkup(Loc.GetString("comp-pda-ui-blank"));
                    }

                    _menu.EjectIDButton.Visible = msg.PDAOwnerInfo.IdOwner != null || msg.PDAOwnerInfo.JobTitle != null;
                    _menu.EjectPenButton.Visible = msg.HasPen;
                    _menu.ActivateUplinkButton.Visible = msg.HasUplink;

                    break;
                }
            }
        }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _menu?.Dispose();
        }

        private class PDAMenu : SS14Window
        {
            public Button FlashLightToggleButton { get; }
            public Button EjectIDButton { get; }
            public Button EjectPenButton { get; }

            public Button ActivateUplinkButton { get; }

            public readonly TabContainer MasterTabContainer;

            public RichTextLabel PDAOwnerLabel { get; }
            public PanelContainer IDInfoContainer { get; }
            public RichTextLabel IDInfoLabel { get; }

            public PDAMenu(PDABoundUserInterface owner, IPrototypeManager prototypeManager)
            {
                MinSize = SetSize = (512, 256);
                Title = Loc.GetString("comp-pda-ui-menu-title");

                #region MAIN_MENU_TAB
                //Main menu
                PDAOwnerLabel = new RichTextLabel
                {
                };

                IDInfoLabel = new RichTextLabel()
                {
                    HorizontalExpand = true,
                };

                EjectIDButton = new Button
                {
                    Text = Loc.GetString("comp-pda-ui-eject-id-button"),
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Center
                };
                EjectPenButton = new Button
                {
                    Text = Loc.GetString("comp-pda-ui-eject-pen-button"),
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Center
                };
                ActivateUplinkButton = new Button
                {
                    Text = Loc.GetString("pda-bound-user-interface-uplink-tab-title")
                };

                var innerHBoxContainer = new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    Children =
                    {
                        IDInfoLabel,
                        EjectIDButton,
                        EjectPenButton
                    }
                };

                IDInfoContainer = new PanelContainer
                {
                    Children =
                    {
                        innerHBoxContainer,
                    }
                };

                FlashLightToggleButton = new Button
                {
                    Text = Loc.GetString("comp-pda-ui-toggle-flashlight-button"),
                    ToggleMode = true,
                };

                var mainMenuTabContainer = new BoxContainer
                {
                    Orientation = LayoutOrientation.Vertical,
                    VerticalExpand = true,
                    HorizontalExpand = true,
                    MinSize = (50, 50),

                    Children =
                    {
                        PDAOwnerLabel,
                        IDInfoContainer,
                        FlashLightToggleButton,
                        ActivateUplinkButton
                    }
                };

                #endregion

                //The master menu that contains all of the tabs.
                MasterTabContainer = new TabContainer
                {
                    Children =
                    {
                        mainMenuTabContainer,
                    }
                };

                //Add all the tabs to the Master container.
                MasterTabContainer.SetTabTitle(0, Loc.GetString("pda-bound-user-interface-main-menu-tab-title"));
                Contents.AddChild(MasterTabContainer);
            }
        }
    }
}
