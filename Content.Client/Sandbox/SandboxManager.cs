using Content.Client.UserInterface;
using Content.Shared.Sandbox;
using Robust.Client.Interfaces.Placement;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
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
#pragma warning restore 649

        private bool _sandboxAllowed;
        private SandboxWindow _window;

        public void Initialize()
        {
            _netManager.RegisterNetMessage<MsgSandboxStatus>(nameof(MsgSandboxStatus),
                message => SetAllowed(message.SandboxAllowed));

            _gameHud.SandboxButtonToggled = SandboxButtonToggled;
        }

        private void SandboxButtonToggled(bool newValue)
        {
            if (newValue)
            {
                if (_sandboxAllowed)
                {
                    OpenWindow();
                }
            }
            else
            {
                _window?.Close();
            }
        }

        private void SetAllowed(bool newAllowed)
        {
            if (newAllowed == _sandboxAllowed)
            {
                return;
            }

            _sandboxAllowed = newAllowed;
            _gameHud.SandboxButtonVisible = newAllowed;

            if (!newAllowed)
            {
                // Sandbox permission revoked, close window.
                _window?.Close();
            }
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

            _window.Open();
        }

        private void WindowOnOnClose()
        {
            _window = null;
            _gameHud.SandboxButtonDown = false;
        }

        private void OnRespawnButtonOnOnPressed(BaseButton.ButtonEventArgs args)
        {
            _netManager.ClientSendMessage(_netManager.CreateNetMessage<MsgSandboxRespawn>());
        }

        private void OnSpawnEntitiesButtonClicked(BaseButton.ButtonEventArgs args)
        {
            var window = new EntitySpawnWindow(_placementManager, _prototypeManager, _resourceCache, _localization);
            window.OpenToLeft();
        }

        private void OnSpawnTilesButtonClicked(BaseButton.ButtonEventArgs args)
        {
            var window = new TileSpawnWindow(_tileDefinitionManager, _placementManager, _resourceCache);
            window.OpenToLeft();
        }
    }
}
