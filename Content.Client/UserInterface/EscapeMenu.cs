using Content.Client.Sandbox;
using Content.Client.UserInterface.AdminMenu;
using Robust.Client.Console;
using Robust.Client.Interfaces.Placement;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface
{
    internal sealed class EscapeMenu : SS14Window
    {
        private readonly IClientConsole _console;
        private readonly ITileDefinitionManager _tileDefinitionManager;
        private readonly IPlacementManager _placementManager;
        private readonly IPrototypeManager _prototypeManager;
        private readonly IResourceCache _resourceCache;
        private readonly IConfigurationManager _configSystem;
        private readonly ILocalizationManager _localizationManager;

        private BaseButton DisconnectButton;
        private BaseButton QuitButton;
        private BaseButton OptionsButton;
        private OptionsMenu optionsMenu;

        public EscapeMenu(IClientConsole console,
            ITileDefinitionManager tileDefinitionManager,
            IPlacementManager placementManager,
            IPrototypeManager prototypeManager,
            IResourceCache resourceCache,
            IConfigurationManager configSystem, ILocalizationManager localizationManager)
        {
            _configSystem = configSystem;
            _localizationManager = localizationManager;
            _console = console;
            _tileDefinitionManager = tileDefinitionManager;
            _placementManager = placementManager;
            _prototypeManager = prototypeManager;
            _resourceCache = resourceCache;

            IoCManager.InjectDependencies(this);

            PerformLayout();
        }

        private void PerformLayout()
        {
            optionsMenu = new OptionsMenu(_configSystem);

            Resizable = false;

            Title = "Esc Menu";

            var vBox = new VBoxContainer {SeparationOverride = 4};
            Contents.AddChild(vBox);

            OptionsButton = new Button {Text = _localizationManager.GetString("Options")};
            OptionsButton.OnPressed += OnOptionsButtonClicked;
            vBox.AddChild(OptionsButton);

            DisconnectButton = new Button {Text = _localizationManager.GetString("Disconnect")};
            DisconnectButton.OnPressed += OnDisconnectButtonClicked;
            vBox.AddChild(DisconnectButton);

            QuitButton = new Button {Text = _localizationManager.GetString("Quit Game")};
            QuitButton.OnPressed += OnQuitButtonClicked;
            vBox.AddChild(QuitButton);

            var adminMenu = IoCManager.Resolve<IAdminMenuManager>();
            if (adminMenu.CanOpen())
            {
                var adminMenuButton = new Button { Text = _localizationManager.GetString("Admin Menu") };
                adminMenuButton.OnPressed += (args) =>
                {
                    adminMenu.Open();
                };
                vBox.AddChild(adminMenuButton);
            }
        }

        private void OnQuitButtonClicked(BaseButton.ButtonEventArgs args)
        {
            _console.ProcessCommand("quit");
            Dispose();
        }

        private void OnDisconnectButtonClicked(BaseButton.ButtonEventArgs args)
        {
            _console.ProcessCommand("disconnect");
            Dispose();
        }

        private void OnOptionsButtonClicked(BaseButton.ButtonEventArgs args)
        {
            optionsMenu.OpenCentered();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                optionsMenu.Dispose();
            }
        }

        public override void Close()
        {
            base.Close();
        }
    }
}
