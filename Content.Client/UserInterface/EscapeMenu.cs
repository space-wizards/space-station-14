using Content.Client.Sandbox;
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
        private readonly ITileDefinitionManager __tileDefinitionManager;
        private readonly IPlacementManager _placementManager;
        private readonly IPrototypeManager _prototypeManager;
        private readonly IResourceCache _resourceCache;
        private readonly IConfigurationManager _configSystem;
        private readonly ILocalizationManager _localizationManager;
#pragma warning disable 649
        [Dependency] private readonly ISandboxManager _sandboxManager;
        [Dependency] private readonly IClientConGroupController _conGroupController;
#pragma warning restore 649

        private BaseButton QuitButton;
        private BaseButton OptionsButton;
        private BaseButton SpawnEntitiesButton;
        private BaseButton SpawnTilesButton;
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
            __tileDefinitionManager = tileDefinitionManager;
            _placementManager = placementManager;
            _prototypeManager = prototypeManager;
            _resourceCache = resourceCache;

            IoCManager.InjectDependencies(this);

            _sandboxManager.AllowedChanged += AllowedChanged;
            _conGroupController.ConGroupUpdated += UpdateSpawnButtonStates;

            PerformLayout();

            UpdateSpawnButtonStates();
        }

        private void PerformLayout()
        {
            optionsMenu = new OptionsMenu(_configSystem);

            Resizable = false;

            Title = "Menu";

            var vBox = new VBoxContainer {SeparationOverride = 6};
            Contents.AddChild(vBox);

            SpawnEntitiesButton = new Button {Text = "Spawn Entities"};
            SpawnEntitiesButton.OnPressed += OnSpawnEntitiesButtonClicked;
            vBox.AddChild(SpawnEntitiesButton);

            SpawnTilesButton = new Button {Text = "Spawn Tiles"};
            SpawnTilesButton.OnPressed += OnSpawnTilesButtonClicked;
            vBox.AddChild(SpawnTilesButton);

            // Add a spacer.
            //vBox.AddChild(new Control { CustomMinimumSize = (0, 5)});

            OptionsButton = new Button {Text = "Options"};
            OptionsButton.OnPressed += OnOptionsButtonClicked;
            vBox.AddChild(OptionsButton);

            QuitButton = new Button {Text = "Quit"};
            QuitButton.OnPressed += OnQuitButtonClicked;
            vBox.AddChild(QuitButton);
        }

        private void OnQuitButtonClicked(BaseButton.ButtonEventArgs args)
        {
            _console.ProcessCommand("disconnect");
            Dispose();
        }

        private void OnOptionsButtonClicked(BaseButton.ButtonEventArgs args)
        {
            optionsMenu.OpenCentered();
        }

        private void OnSpawnEntitiesButtonClicked(BaseButton.ButtonEventArgs args)
        {
            var window = new EntitySpawnWindow(_placementManager, _prototypeManager, _resourceCache,
                _localizationManager);
            window.OpenToLeft();
        }

        private void OnSpawnTilesButtonClicked(BaseButton.ButtonEventArgs args)
        {
            var window = new TileSpawnWindow(__tileDefinitionManager, _placementManager, _resourceCache);
            window.OpenToLeft();
        }

        private void UpdateSpawnButtonStates()
        {
            if (_conGroupController.CanAdminPlace() || _sandboxManager.SandboxAllowed)
            {
                SpawnEntitiesButton.Disabled = false;
                SpawnTilesButton.Disabled = false;
            }
            else
            {
                SpawnEntitiesButton.Disabled = true;
                SpawnTilesButton.Disabled = true;
            }
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

            _sandboxManager.AllowedChanged -= AllowedChanged;
            _conGroupController.ConGroupUpdated -= UpdateSpawnButtonStates;
        }

        private void AllowedChanged(bool newAllowed) => UpdateSpawnButtonStates();
    }
}
