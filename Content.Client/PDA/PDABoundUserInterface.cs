using Content.Client.CartridgeLoader;
using Content.Shared.CartridgeLoader;
using Content.Shared.CCVar;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.CrewManifest;
using Content.Shared.PDA;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;

namespace Content.Client.PDA
{
    [UsedImplicitly]
    public sealed class PDABoundUserInterface : CartridgeLoaderBoundUserInterface
    {
        [Dependency] private readonly IEntityManager? _entityManager = default!;
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

            _menu.ActivateMusicButton.OnPressed += _ =>
            {
                SendMessage(new PDAShowMusicMessage());
            };

            _menu.AccessRingtoneButton.OnPressed += _ =>
            {
                SendMessage(new PDAShowRingtoneMessage());
            };

            _menu.ShowUplinkButton.OnPressed += _ =>
            {
                SendMessage(new PDAShowUplinkMessage());
            };

            _menu.LockUplinkButton.OnPressed += _ =>
            {
                SendMessage(new PDALockUplinkMessage());
            };

            _menu.OnProgramItemPressed += ActivateCartridge;
            _menu.OnInstallButtonPressed += InstallCartridge;
            _menu.OnUninstallButtonPressed += UninstallCartridge;
            _menu.ProgramCloseButton.OnPressed += _ => DeactivateActiveCartridge();

            var borderColorComponent = GetBorderColorComponent();
            if (borderColorComponent == null)
                return;

            _menu.BorderColor = borderColorComponent.BorderColor;
            _menu.AccentHColor = borderColorComponent.AccentHColor;
            _menu.AccentVColor = borderColorComponent.AccentVColor;
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not PDAUpdateState updateState)
                return;

            _menu?.UpdateState(updateState);
        }


        protected override void AttachCartridgeUI(Control cartridgeUIFragment, string? title)
        {
            _menu?.ProgramView.AddChild(cartridgeUIFragment);
            _menu?.ToProgramView(title ?? Loc.GetString("comp-pda-io-program-fallback-title"));
        }

        protected override void DetachCartridgeUI(Control cartridgeUIFragment)
        {
            if (_menu is null)
                return;

            _menu.ToHomeScreen();
            _menu.HideProgramHeader();
            _menu.ProgramView.RemoveChild(cartridgeUIFragment);
        }

        protected override void UpdateAvailablePrograms(List<(EntityUid, CartridgeComponent)> programs)
        {
            _menu?.UpdateAvailablePrograms(programs);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _menu?.Dispose();
        }

        private PDABorderColorComponent? GetBorderColorComponent()
        {
            return _entityManager?.GetComponentOrNull<PDABorderColorComponent>(Owner.Owner);
        }
    }
}
