using Content.Client.Administration.Managers;
using Content.Client.Decals.UI;
using Content.Client.HUD;
using Content.Client.Markers;
using Content.Client.SubFloor;
using Content.Shared.Administration;
using Content.Shared.GameTicking;
using Content.Shared.Input;
using Content.Shared.Sandbox;
using Robust.Client.Console;
using Robust.Client.Debugging;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Placement;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Network;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Sandbox
{
    // Layout for the SandboxWindow
    public sealed class SandboxWindow : DefaultWindow
    {
        public readonly Button RespawnButton;
        public readonly Button SpawnEntitiesButton;
        public readonly Button SpawnTilesButton;
        public readonly Button SpawnDecalsButton;
        public readonly Button GiveFullAccessButton;  //A button that just puts a captain's ID in your hands.
        public readonly Button GiveAghostButton;
        public readonly Button ToggleLightButton;
        public readonly Button ToggleFovButton;
        public readonly Button ToggleShadowsButton;
        public readonly Button SuicideButton;
        public readonly Button ToggleSubfloorButton;
        public readonly Button ShowMarkersButton; //Shows spawn points
        public readonly Button ShowBbButton; //Shows bounding boxes
        public readonly Button MachineLinkingButton; // Enables/disables machine linking mode.
        private readonly IGameHud _gameHud;

        public SandboxWindow()
        {
            Resizable = false;
            _gameHud = IoCManager.Resolve<IGameHud>();

            Title = Loc.GetString("sandbox-window-title");

            var vBox = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                SeparationOverride = 4
            };
            Contents.AddChild(vBox);

            RespawnButton = new Button { Text = Loc.GetString("sandbox-window-respawn-button") };
            vBox.AddChild(RespawnButton);

            SpawnEntitiesButton = new Button { Text = Loc.GetString("sandbox-window-spawn-entities-button") };
            vBox.AddChild(SpawnEntitiesButton);

            SpawnTilesButton = new Button { Text = Loc.GetString("sandbox-window-spawn-tiles-button") };
            vBox.AddChild(SpawnTilesButton);

            SpawnDecalsButton = new Button { Text = Loc.GetString("sandbox-window-spawn-decals-button") };
            vBox.AddChild(SpawnDecalsButton);

            GiveFullAccessButton = new Button { Text = Loc.GetString("sandbox-window-grant-full-access-button") };
            vBox.AddChild(GiveFullAccessButton);

            GiveAghostButton = new Button { Text = Loc.GetString("sandbox-window-ghost-button") };
            vBox.AddChild(GiveAghostButton);

            ToggleLightButton = new Button { Text = Loc.GetString("sandbox-window-toggle-lights-button"), ToggleMode = true, Pressed = !IoCManager.Resolve<ILightManager>().Enabled };
            vBox.AddChild(ToggleLightButton);

            ToggleFovButton = new Button { Text = Loc.GetString("sandbox-window-toggle-fov-button"), ToggleMode = true, Pressed = !IoCManager.Resolve<IEyeManager>().CurrentEye.DrawFov };
            vBox.AddChild(ToggleFovButton);

            ToggleShadowsButton = new Button { Text = Loc.GetString("sandbox-window-toggle-shadows-button"), ToggleMode = true, Pressed = !IoCManager.Resolve<ILightManager>().DrawShadows };
            vBox.AddChild(ToggleShadowsButton);

            ToggleSubfloorButton = new Button { Text = Loc.GetString("sandbox-window-toggle-subfloor-button"), ToggleMode = true, Pressed = EntitySystem.Get<SubFloorHideSystem>().ShowAll };
            vBox.AddChild(ToggleSubfloorButton);

            SuicideButton = new Button { Text = Loc.GetString("sandbox-window-toggle-suicide-button") };
            vBox.AddChild(SuicideButton);

            ShowMarkersButton = new Button { Text = Loc.GetString("sandbox-window-show-spawns-button"), ToggleMode = true, Pressed = EntitySystem.Get<MarkerSystem>().MarkersVisible };
            vBox.AddChild(ShowMarkersButton);

            ShowBbButton = new Button { Text = Loc.GetString("sandbox-window-show-bb-button"), ToggleMode = true, Pressed = (EntitySystem.Get<DebugPhysicsSystem>().Flags & PhysicsDebugFlags.Shapes) != 0x0 };
            vBox.AddChild(ShowBbButton);

            MachineLinkingButton = new Button { Text = Loc.GetString("sandbox-window-link-machines-button"), ToggleMode = true };
            vBox.AddChild(MachineLinkingButton);
        }


        protected override void EnteredTree()
        {
            base.EnteredTree();
            _gameHud.SandboxButtonDown = true;
        }

        protected override void ExitedTree()
        {
            base.ExitedTree();
            _gameHud.SandboxButtonDown = false;
        }

    }

    public sealed class SandboxSystem : SharedSandboxSystem
    {
        [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
        [Dependency] private readonly IGameHud _gameHud = default!;
        [Dependency] private readonly IPlacementManager _placementManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IClientAdminManager _adminManager = default!;

        public bool SandboxAllowed { get; private set; }

        private SandboxWindow? _sandboxWindow;
        private EntitySpawnWindow? _spawnWindow;
        private TileSpawnWindow? _tilesSpawnWindow;
        private DecalPlacerWindow? _decalSpawnWindow;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<MsgSandboxStatus>(OnSandboxStatus);
            SubscribeNetworkEvent<RoundRestartCleanupEvent>(OnRoundRestart);

            _adminManager.AdminStatusUpdated += OnAdminStatus;
            _gameHud.SandboxButtonToggled += SandboxButtonPressed;

            // Do these need cleanup?
            _inputManager.SetInputCommand(ContentKeyFunctions.OpenEntitySpawnWindow,
                InputCmdHandler.FromDelegate(session => ToggleEntitySpawnWindow()));
            _inputManager.SetInputCommand(ContentKeyFunctions.OpenSandboxWindow,
                InputCmdHandler.FromDelegate(session => ToggleSandboxWindow()));
            _inputManager.SetInputCommand(ContentKeyFunctions.OpenTileSpawnWindow,
                InputCmdHandler.FromDelegate(session => ToggleTilesWindow()));
            _inputManager.SetInputCommand(ContentKeyFunctions.OpenDecalSpawnWindow,
                InputCmdHandler.FromDelegate(session => ToggleDecalsWindow()));
        }

        private void OnAdminStatus()
        {
            if (CanSandbox())
                Enable();
            else
                Disable();
        }

        private bool CanSandbox()
        {
            return SandboxAllowed || _adminManager.IsActive();
        }

        /// <summary>
        /// Run when sandbox is disabled
        /// </summary>
        private void Disable()
        {
            _gameHud.SandboxButtonVisible = false;
            _sandboxWindow?.Close();
            _sandboxWindow = null;
            _spawnWindow?.Close();
            _tilesSpawnWindow?.Close();
            _decalSpawnWindow?.Close();
        }

        private void Enable()
        {
            _gameHud.SandboxButtonVisible = true;
        }

        private void OnRoundRestart(RoundRestartCleanupEvent ev)
        {
            // Go through and cleanup windows (even if they remain adminned better to just shut them).
            Disable();

            if (CanSandbox())
                Enable();
        }

        public override void Shutdown()
        {
            base.Shutdown();
            // TODO: Gamehud moment
            _gameHud.SandboxButtonToggled -= SandboxButtonPressed;
            _adminManager.AdminStatusUpdated -= OnAdminStatus;
        }

        private void OnSandboxStatus(MsgSandboxStatus ev)
        {
            SetAllowed(ev.SandboxAllowed);
        }

        private void SandboxButtonPressed(bool newValue)
        {
            UpdateSandboxWindowVisibility();
        }

        private void ToggleSandboxWindow()
        {
            UpdateSandboxWindowVisibility();
        }

        private void UpdateSandboxWindowVisibility()
        {
            if (CanSandbox() && _sandboxWindow?.IsOpen != true)
                OpenSandboxWindow();
            else
                _sandboxWindow?.Close();
        }

        private void SetAllowed(bool newAllowed)
        {
            if (newAllowed == SandboxAllowed)
                return;

            SandboxAllowed = newAllowed;
            _gameHud.SandboxButtonVisible = CanSandbox();

            if (!CanSandbox())
                Disable();
        }

        private void OpenSandboxWindow()
        {
            if (_sandboxWindow != null)
            {
                if (!_sandboxWindow.IsOpen)
                    _sandboxWindow.Open();

                return;
            }

            _sandboxWindow = new SandboxWindow();

            _sandboxWindow.OnClose += SandboxWindowOnClose;

            _sandboxWindow.RespawnButton.OnPressed += OnRespawnButtonOnOnPressed;
            _sandboxWindow.SpawnTilesButton.OnPressed += OnSpawnTilesButtonClicked;
            _sandboxWindow.SpawnEntitiesButton.OnPressed += OnSpawnEntitiesButtonClicked;
            _sandboxWindow.SpawnDecalsButton.OnPressed += OnSpawnDecalsButtonClicked;
            _sandboxWindow.GiveFullAccessButton.OnPressed += OnGiveAdminAccessButtonClicked;
            _sandboxWindow.GiveAghostButton.OnPressed += OnGiveAghostButtonClicked;
            _sandboxWindow.ToggleLightButton.OnToggled += OnToggleLightButtonClicked;
            _sandboxWindow.ToggleFovButton.OnToggled += OnToggleFovButtonClicked;
            _sandboxWindow.ToggleShadowsButton.OnToggled += OnToggleShadowsButtonClicked;
            _sandboxWindow.SuicideButton.OnPressed += OnSuicideButtonClicked;
            _sandboxWindow.ToggleSubfloorButton.OnPressed += OnToggleSubfloorButtonClicked;
            _sandboxWindow.ShowMarkersButton.OnPressed += OnShowMarkersButtonClicked;
            _sandboxWindow.ShowBbButton.OnPressed += OnShowBbButtonClicked;
            _sandboxWindow.MachineLinkingButton.OnPressed += OnMachineLinkingButtonClicked;

            _sandboxWindow.OpenCentered();
        }

        private void SandboxWindowOnClose()
        {
            _sandboxWindow = null;
        }

        private void OnRespawnButtonOnOnPressed(BaseButton.ButtonEventArgs args)
        {
            RaiseNetworkEvent(new MsgSandboxRespawn());
        }

        private void OnSpawnEntitiesButtonClicked(BaseButton.ButtonEventArgs args)
        {
            ToggleEntitySpawnWindow();
        }

        private void OnSpawnTilesButtonClicked(BaseButton.ButtonEventArgs args)
        {
            ToggleTilesWindow();
        }

        private void OnSpawnDecalsButtonClicked(BaseButton.ButtonEventArgs obj)
        {
            ToggleDecalsWindow();
        }

        private void OnToggleLightButtonClicked(BaseButton.ButtonEventArgs args)
        {
            ToggleLight();
        }

        private void OnToggleFovButtonClicked(BaseButton.ButtonEventArgs args)
        {
            ToggleFov();
        }

        private void OnToggleShadowsButtonClicked(BaseButton.ButtonEventArgs args)
        {
            ToggleShadows();
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
        private void OnMachineLinkingButtonClicked(BaseButton.ButtonEventArgs args)
        {
            LinkMachines();
        }

        private void OnGiveAdminAccessButtonClicked(BaseButton.ButtonEventArgs args)
        {
            RaiseNetworkEvent(new MsgSandboxGiveAccess());
        }

        private void OnGiveAghostButtonClicked(BaseButton.ButtonEventArgs args)
        {
            RaiseNetworkEvent(new MsgSandboxGiveAghost());
        }

        private void OnSuicideButtonClicked(BaseButton.ButtonEventArgs args)
        {
            RaiseNetworkEvent(new MsgSandboxSuicide());
        }

        // TODO: These should check for command perms + be reset if the round is over.
        public void ToggleEntitySpawnWindow()
        {
            if (_spawnWindow == null)
            {
                if (!CanSandbox()) return;

                _spawnWindow = new EntitySpawnWindow(_placementManager, PrototypeManager, _resourceCache);
                _spawnWindow.OpenToLeft();
                return;
            }

            if (_spawnWindow.IsOpen)
            {
                _spawnWindow.Close();
            }
            else
            {
                _spawnWindow.Open();
            }
        }

        public void ToggleTilesWindow()
        {
            if (_tilesSpawnWindow == null)
            {
                if (!CanSandbox()) return;

                _tilesSpawnWindow = new TileSpawnWindow(_tileDefinitionManager, _placementManager, _resourceCache);
                _tilesSpawnWindow.OpenToLeft();
                return;
            }

            if (_tilesSpawnWindow.IsOpen)
            {
                _tilesSpawnWindow.Close();
            }
            else
            {
                _tilesSpawnWindow.Open();
            }
        }

        public void ToggleDecalsWindow()
        {
            if (_decalSpawnWindow == null)
            {
                if (!CanSandbox()) return;

                _decalSpawnWindow = new DecalPlacerWindow(PrototypeManager);
                _decalSpawnWindow.OpenToLeft();
                return;
            }

            if (_decalSpawnWindow.IsOpen)
            {
                _decalSpawnWindow.Close();
            }
            else
            {
                _decalSpawnWindow.Open();
            }
        }

        // TODO: need to cleanup these
        private void ToggleLight()
        {
            _consoleHost.ExecuteCommand("togglelight");
        }

        private void ToggleFov()
        {
            _consoleHost.ExecuteCommand("togglefov");
        }

        private void ToggleShadows()
        {
            _consoleHost.ExecuteCommand("toggleshadows");
        }

        private void ToggleSubFloor()
        {
            _consoleHost.ExecuteCommand("showsubfloor");
        }

        private void ShowMarkers()
        {
            _consoleHost.ExecuteCommand("showmarkers");
        }

        private void ShowBb()
        {
            _consoleHost.ExecuteCommand("physics shapes");
        }

        private void LinkMachines()
        {
            _consoleHost.ExecuteCommand("signallink");
        }
    }
}
