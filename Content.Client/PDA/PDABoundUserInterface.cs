using Content.Client.Message;
using Content.Shared.CCVar;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.CrewManifest;
using Content.Shared.PDA;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Configuration;

namespace Content.Client.PDA
{
    [UsedImplicitly]
    public sealed class PDABoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IConfigurationManager _configManager = default!;

        private PDAMenu? _menu;

        public PDABoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
            IoCManager.InjectDependencies(this);
        }

        protected override void Open()
        {
            base.Open();
            SendMessage(new PDARequestUpdateInterfaceMessage());
            _menu = new PDAMenu();
            _menu.OpenCenteredLeft();
            _menu.OnClose += Close;
            _menu.FlashLightToggleButton.OnToggled += _ =>
            {
                SendMessage(new PDAToggleFlashlightMessage());
            };

            if (_configManager.GetCVar(CCVars.CrewManifestUnsecure))
            {
                _menu.CrewManifestButton.Visible = true;
                _menu.CrewManifestButton.OnPressed += _ =>
                {
                    SendMessage(new CrewManifestOpenUiMessage());
                };
            }

            _menu.EjectIdButton.OnPressed += _ =>
            {
                SendMessage(new ItemSlotButtonPressedEvent(PDAComponent.PDAIdSlotId));
            };

            _menu.EjectPenButton.OnPressed += _ =>
            {
                SendMessage(new ItemSlotButtonPressedEvent(PDAComponent.PDAPenSlotId));
            };

            _menu.ActivateUplinkButton.OnPressed += _ =>
            {
                SendMessage(new PDAShowUplinkMessage());
            };

            _menu.ActivateMusicButton.OnPressed += _ =>
            {
                SendMessage(new PDAShowMusicMessage());
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
                            ("Owner",msg.PDAOwnerInfo.IdOwner ?? Loc.GetString("comp-pda-ui-unknown")),
                            ("JobTitle",msg.PDAOwnerInfo.JobTitle ?? Loc.GetString("comp-pda-ui-unassigned"))));
                    }
                    else
                    {
                        _menu.IdInfoLabel.SetMarkup(Loc.GetString("comp-pda-ui-blank"));
                    }

                    _menu.StationNameLabel.SetMarkup(Loc.GetString("comp-pda-ui-station", ("Station",msg.StationName ?? Loc.GetString("comp-pda-ui-unknown"))));

                    _menu.EjectIdButton.Visible = msg.PDAOwnerInfo.IdOwner != null || msg.PDAOwnerInfo.JobTitle != null;
                    _menu.EjectPenButton.Visible = msg.HasPen;
                    _menu.ActivateUplinkButton.Visible = msg.HasUplink;
                    _menu.ActivateMusicButton.Visible = msg.CanPlayMusic;

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
