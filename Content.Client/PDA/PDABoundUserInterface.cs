using Content.Client.CartridgeLoader;
using Content.Client.Message;
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
        [Dependency] private readonly IConfigurationManager _configManager = default!;

        private PDAMenu? _menu;
        //private List<EntityUid> _availablePrograms = new();

        public PDABoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
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

            //Refresh installation status of available programs
            /*_menu.ProgramListButton.OnPressed += _ =>
            {
                var programs = GetCartridgeComponents(_availablePrograms);
                _menu.UpdateAvailablePrograms(programs);
            };*/

            _menu.OnProgramItemPressed += ActivateCartridge;
            _menu.OnInstallButtonPressed += InstallCartridge;
            _menu.OnUninstallButtonPressed += UninstallCartridge;
            _menu.ProgramCloseButton.OnPressed += _ => DeactivateActiveCartridge();

        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not PDAUpdateState updateState)
                return;

            _menu?.UpdateState(updateState);
        }


        protected override void AttachCartridgeUI(Control cartridgeUI, string? title)
        {
            _menu?.ProgramView.AddChild(cartridgeUI);
            _menu?.ToProgramView(title ?? Loc.GetString("comp-pda-io-program-fallback-title"));

        }

        protected override void DetachCartridgeUI(Control cartridgeUI)
        {
            if (_menu is null)
                return;

            _menu.ToHomeScreen();
            _menu.HideProgramHeader();
            _menu.ProgramView.RemoveChild(cartridgeUI);
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
    }
}
