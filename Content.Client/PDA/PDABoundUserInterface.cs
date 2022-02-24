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
    public sealed class PDABoundUserInterface : BoundUserInterface
    {
        private PDAMenu? _menu;

        public PDABoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            SendMessage(new PDARequestUpdateInterfaceMessage());
            _menu = new PDAMenu();
            _menu.OpenToLeft();
            _menu.OnClose += Close;
            _menu.FlashLightToggleButton.OnToggled += _ =>
            {
                SendMessage(new PDAToggleFlashlightMessage());
            };

            _menu.EjectIdButton.OnPressed += _ =>
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

            _menu.AccessRingtoneButton.OnPressed += _ =>
            {
                SendMessage(new PDAShowRingtoneMessage());
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
                        _menu.PdaOwnerLabel.SetMarkup(Loc.GetString("comp-pda-ui-owner",
                            ("ActualOwnerName", msg.PDAOwnerInfo.ActualOwnerName)));
                    }


                    if (msg.PDAOwnerInfo.IdOwner != null || msg.PDAOwnerInfo.JobTitle != null)
                    {
                        _menu.IdInfoLabel.SetMarkup(Loc.GetString("comp-pda-ui",
                            ("Owner",msg.PDAOwnerInfo.IdOwner ?? "Unknown"),
                            ("JobTitle",msg.PDAOwnerInfo.JobTitle ?? "Unassigned")));
                    }
                    else
                    {
                        _menu.IdInfoLabel.SetMarkup(Loc.GetString("comp-pda-ui-blank"));
                    }

                    _menu.EjectIdButton.Visible = msg.PDAOwnerInfo.IdOwner != null || msg.PDAOwnerInfo.JobTitle != null;
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
    }
}
