using Robust.Client.Console;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.Placement;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.Map;
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
            IConfigurationManager configSystem)
        {
            _configSystem = configSystem;
            _console = console;
            __tileDefinitionManager = tileDefinitionManager;
            _placementManager = placementManager;
            _prototypeManager = prototypeManager;
            _resourceCache = resourceCache;

            PerformLayout();
        }

        private void PerformLayout()
        {
            optionsMenu = new OptionsMenu(_configSystem)
            {
                Visible = false
            };
            optionsMenu.AddToScreen();

            Resizable = false;
            HideOnClose = true;

            Title = "Menu";

            var vBox = new VBoxContainer {SeparationOverride = 2};
            Contents.AddChild(vBox);

            SpawnEntitiesButton = new Button {Text = "Spawn Entities"};
            SpawnEntitiesButton.OnPressed += OnSpawnEntitiesButtonClicked;
            vBox.AddChild(SpawnEntitiesButton);

            SpawnTilesButton = new Button {Text = "Spawn Tiles"};
            SpawnTilesButton.OnPressed += OnSpawnTilesButtonClicked;
            vBox.AddChild(SpawnTilesButton);

            // Add a spacer.
            vBox.AddChild(new Control { CustomMinimumSize = (0, 5)});

            OptionsButton = new Button {Text = "Options"};
            OptionsButton.OnPressed += OnOptionsButtonClicked;
            vBox.AddChild(OptionsButton);

            QuitButton = new Button {Text = "Quit"};
            QuitButton.OnPressed += OnQuitButtonClicked;
            vBox.AddChild(QuitButton);

            Size = CombinedMinimumSize;
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
            var window = new EntitySpawnWindow(_placementManager, _prototypeManager, _resourceCache);
            window.AddToScreen();
            window.OpenToLeft();
        }

        private void OnSpawnTilesButtonClicked(BaseButton.ButtonEventArgs args)
        {
            var window = new TileSpawnWindow(__tileDefinitionManager, _placementManager, _resourceCache);
            window.AddToScreen();
            window.OpenToLeft();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                optionsMenu.Dispose();
            }
        }
    }
}
