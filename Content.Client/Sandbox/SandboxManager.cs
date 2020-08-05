using System;
using Content.Client.UserInterface;
using Content.Shared.Input;
using Content.Shared.Sandbox;
using Robust.Client.Interfaces.Console;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.Graphics.Lighting;
using Robust.Client.Interfaces.Placement;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.Client.Sandbox
{
    internal sealed class SandboxManager : SharedSandboxManager, ISandboxManager
    {
#pragma warning disable 649
        [Dependency] private readonly IGameHud _gameHud;
        [Dependency] private readonly IClientNetManager _netManager;
        [Dependency] private readonly ILocalizationManager _localization;
        [Dependency] private readonly IPlacementManager _placementManager;
        [Dependency] private readonly IPrototypeManager _prototypeManager;
        [Dependency] private readonly IResourceCache _resourceCache;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager;
        [Dependency] private readonly IInputManager _inputManager;
#pragma warning restore 649

        public bool SandboxAllowed { get; private set; }

        public event Action<bool> AllowedChanged;

        private SandboxWindow _window;
        private EntitySpawnWindow _spawnWindow;
        private TileSpawnWindow _tilesSpawnWindow;
        private bool _sandboxWindowToggled;

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

            _window = new SandboxWindow(_localization);

            _window.OnClose += WindowOnOnClose;

            _window.RespawnButton.OnPressed += OnRespawnButtonOnOnPressed;
            _window.SpawnTilesButton.OnPressed += OnSpawnTilesButtonClicked;
            _window.SpawnEntitiesButton.OnPressed += OnSpawnEntitiesButtonClicked;
            _window.GiveFullAccessButton.OnPressed += OnGiveAdminAccessButtonClicked;
            _window.GiveAghostButton.OnPressed += OnGiveAghostButtonClicked;
            _window.ToggleLightButton.OnPressed += OnToggleLightButtonClicked;
            _window.SuicideButton.OnPressed += OnSuicideButtonClicked;

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

        private bool ToggleLight()
        {
            var mgr = IoCManager.Resolve<ILightManager>();
            mgr.Enabled = !mgr.Enabled;
            return false;
        }
    }
}
