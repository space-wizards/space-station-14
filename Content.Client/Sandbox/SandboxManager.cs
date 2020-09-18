using System;
using Content.Client.UserInterface;
using Content.Shared.Input;
using Content.Shared.Sandbox;
using Robust.Client.Console;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.Placement;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Input.Binding;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.Client.Sandbox
{
    // Layout for the SandboxWindow
    public class SandboxWindow : SS14Window
    {
        public Button RespawnButton;
        public Button SpawnEntitiesButton;
        public Button SpawnTilesButton;
        public Button GiveFullAccessButton;  //A button that just puts a captain's ID in your hands.
        public Button GiveAghostButton;
        public Button ToggleLightButton;
        public Button SuicideButton;
        public Button ToggleSubfloorButton;
        public Button ShowMarkersButton; //Shows spawn points
        public Button ShowBbButton; //Shows bounding boxes

        public SandboxWindow()
        {
            Resizable = false;

            Title = "Sandbox Panel";

            var vBox = new VBoxContainer { SeparationOverride = 4 };
            Contents.AddChild(vBox);

            RespawnButton = new Button { Text = Loc.GetString("Respawn") };
            vBox.AddChild(RespawnButton);

            SpawnEntitiesButton = new Button { Text = Loc.GetString("Spawn Entities") };
            vBox.AddChild(SpawnEntitiesButton);

            SpawnTilesButton = new Button { Text = Loc.GetString("Spawn Tiles") };
            vBox.AddChild(SpawnTilesButton);

            GiveFullAccessButton = new Button { Text = Loc.GetString("Give AA Id") };
            vBox.AddChild(GiveFullAccessButton);

            GiveAghostButton = new Button { Text = Loc.GetString("Ghost") };
            vBox.AddChild(GiveAghostButton);

            ToggleLightButton = new Button { Text = Loc.GetString("Toggle Lights"), ToggleMode = true };
            vBox.AddChild(ToggleLightButton);

            ToggleSubfloorButton = new Button { Text = Loc.GetString("Toggle Subfloor"), ToggleMode = true };
            vBox.AddChild(ToggleSubfloorButton);

            SuicideButton = new Button { Text = Loc.GetString("Suicide") };
            vBox.AddChild(SuicideButton);

            ShowMarkersButton = new Button { Text = Loc.GetString("Show Spawns"), ToggleMode = true };
            vBox.AddChild(ShowMarkersButton);

            ShowBbButton = new Button { Text = Loc.GetString("Show Bb"), ToggleMode = true };
            vBox.AddChild(ShowBbButton);
        }
    }

    internal class SandboxManager : SharedSandboxManager, ISandboxManager
    {
        [Dependency] private readonly IClientConsole _console = default!;
        [Dependency] private readonly IGameHud _gameHud = default!;
        [Dependency] private readonly IClientNetManager _netManager = default!;
        [Dependency] private readonly ILocalizationManager _localization = default!; 
        [Dependency] private readonly IPlacementManager _placementManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;

        public bool SandboxAllowed { get; private set; }

        public event Action<bool> AllowedChanged;

        private SandboxWindow _window;
        private EntitySpawnWindow _spawnWindow;
        private TileSpawnWindow _tilesSpawnWindow;
        private bool _sandboxWindowToggled;
        private bool SpawnEntitiesButton { get; set; }

        public void Initialize()
        {
            _netManager.RegisterNetMessage<MsgSandboxStatus>(nameof(MsgSandboxStatus),
                message => SetAllowed(message.SandboxAllowed));

            _netManager.RegisterNetMessage<MsgSandboxGiveAccess>(nameof(MsgSandboxGiveAccess));

            _netManager.RegisterNetMessage<MsgSandboxRespawn>(nameof(MsgSandboxRespawn));

            _netManager.RegisterNetMessage<MsgSandboxGiveAghost>(nameof(MsgSandboxGiveAghost));

            _netManager.RegisterNetMessage<MsgSandboxSuicide>(nameof(MsgSandboxSuicide));

            _gameHud.SandboxButtonToggled = SandboxButtonPressed;

            _inputManager.SetInputCommand(ContentKeyFunctions.OpenEntitySpawnWindow,
                InputCmdHandler.FromDelegate(session => ToggleEntitySpawnWindow()));
            _inputManager.SetInputCommand(ContentKeyFunctions.OpenSandboxWindow,
                InputCmdHandler.FromDelegate(session => ToggleSandboxWindow()));
            _inputManager.SetInputCommand(ContentKeyFunctions.OpenTileSpawnWindow,
                InputCmdHandler.FromDelegate(session => ToggleTilesWindow()));
        }

        private void SandboxButtonPressed(bool newValue)
        {
            _sandboxWindowToggled = newValue;
            UpdateSandboxWindowVisibility();
        }

        private void ToggleSandboxWindow()
        {
            _sandboxWindowToggled = !_sandboxWindowToggled;
            UpdateSandboxWindowVisibility();
        }

        private void UpdateSandboxWindowVisibility()
        {
            if (_sandboxWindowToggled && SandboxAllowed)
                OpenWindow();
            else
                _window?.Close();
        }

        private void SetAllowed(bool newAllowed)
        {
            if (newAllowed == SandboxAllowed)
            {
                return;
            }

            SandboxAllowed = newAllowed;
            _gameHud.SandboxButtonVisible = newAllowed;

            if (!newAllowed)
            {
                // Sandbox permission revoked, close window.
                _window?.Close();
            }

            AllowedChanged?.Invoke(newAllowed);
        }

        private void OpenWindow()
        {
            if (_window != null)
            {
                return;
            }

            _window = new SandboxWindow();

            _window.OnClose += WindowOnOnClose;

            _window.RespawnButton.OnPressed += OnRespawnButtonOnOnPressed;
            _window.SpawnTilesButton.OnPressed += OnSpawnTilesButtonClicked;
            _window.SpawnEntitiesButton.OnPressed += OnSpawnEntitiesButtonClicked;
            _window.GiveFullAccessButton.OnPressed += OnGiveAdminAccessButtonClicked;
            _window.GiveAghostButton.OnPressed += OnGiveAghostButtonClicked;
            _window.ToggleLightButton.OnToggled += OnToggleLightButtonClicked;
            _window.SuicideButton.OnPressed += OnSuicideButtonClicked;
            _window.ToggleSubfloorButton.OnPressed += OnToggleSubfloorButtonClicked;
            _window.ShowMarkersButton.OnPressed += OnShowMarkersButtonClicked;
            _window.ShowBbButton.OnPressed += OnShowBbButtonClicked;

            _window.OpenCentered();
        }

        private void WindowOnOnClose()
        {
            _window = null;
            _gameHud.SandboxButtonDown = false;
            _sandboxWindowToggled = false;
        }

        private void OnRespawnButtonOnOnPressed(BaseButton.ButtonEventArgs args)
        {
            _netManager.ClientSendMessage(_netManager.CreateNetMessage<MsgSandboxRespawn>());
        }

        private void OnSpawnEntitiesButtonClicked(BaseButton.ButtonEventArgs args)
        {
            ToggleEntitySpawnWindow();
        }

        private void OnSpawnTilesButtonClicked(BaseButton.ButtonEventArgs args)
        {
            ToggleTilesWindow();
        }

        private void OnToggleLightButtonClicked(BaseButton.ButtonEventArgs args)
        {
            ToggleLight();
        }

        private void OnToggleSubfloorButtonClicked(BaseButton.ButtonEventArgs args)
        {
            ToggleSubFloor();
        }

        private void OnShowMarkersButtonClicked(BaseButton.ButtonEventArgs args)
        {
            ShowMarkers();
        }

        private void OnShowBbButtonClicked(BaseButton.ButtonEventArgs args)
        {
            ShowBb();
        }

        private void OnGiveAdminAccessButtonClicked(BaseButton.ButtonEventArgs args)
        {
            _netManager.ClientSendMessage(_netManager.CreateNetMessage<MsgSandboxGiveAccess>());
        }

        private void OnGiveAghostButtonClicked(BaseButton.ButtonEventArgs args)
        {
            _netManager.ClientSendMessage(_netManager.CreateNetMessage<MsgSandboxGiveAghost>());
        }

        private void OnSuicideButtonClicked(BaseButton.ButtonEventArgs args)
        {
            _netManager.ClientSendMessage(_netManager.CreateNetMessage<MsgSandboxSuicide>());
        }

        private void ToggleEntitySpawnWindow()
        {
            if (_spawnWindow == null)
                _spawnWindow = new EntitySpawnWindow(_placementManager, _prototypeManager, _resourceCache, _localization);

            if (_spawnWindow.IsOpen)
            {
                _spawnWindow.Close();
            }
            else
            {
                _spawnWindow = new EntitySpawnWindow(_placementManager, _prototypeManager, _resourceCache, _localization);
                _spawnWindow.OpenToLeft();
            }
        }

        private void ToggleTilesWindow()
        {
            if (_tilesSpawnWindow == null)
                _tilesSpawnWindow = new TileSpawnWindow(_tileDefinitionManager, _placementManager, _resourceCache);

            if (_tilesSpawnWindow.IsOpen)
            {
                _tilesSpawnWindow.Close();
            }
            else
            {
                _tilesSpawnWindow = new TileSpawnWindow(_tileDefinitionManager, _placementManager, _resourceCache);
                _tilesSpawnWindow.OpenToLeft();
            }
        }

        private void ToggleLight()
        {
            _console.ProcessCommand("togglelight");
        }

        private void ToggleSubFloor()
        {
            _console.ProcessCommand("showsubfloor");
        }

        private void ShowMarkers()
        {
            _console.ProcessCommand("showmarkers");
        }

        private void ShowBb()
        {
            _console.ProcessCommand("showbb");
        }
    }
}
