using Content.Client.Administration.Managers;
using Content.Client.HUD;
using Content.Shared.GameTicking;
using Content.Shared.Input;
using Content.Shared.Sandbox;
using Robust.Client.Console;
using Robust.Client.Input;
using Robust.Client.Placement;
using Robust.Client.ResourceManagement;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;

namespace Content.Client.Sandbox
{
    public sealed class SandboxSystem : SharedSandboxSystem
    {
        [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
        [Dependency] private readonly IGameHud _gameHud = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPlacementManager _placementManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IClientAdminManager _adminManager = default!;

        public bool SandboxAllowed { get; private set; }

        public event Action? SandboxEnabled;
        public event Action? SandboxDisabled;

        public override void Initialize()
        {
            SubscribeNetworkEvent<MsgSandboxStatus>(OnSandboxStatus);
            SubscribeNetworkEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        }

        private void CheckStatus()
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

        private void Enable()
        {
            SandboxEnabled?.Invoke();
        }

        /// <summary>
        /// Run when sandbox is disabled
        /// </summary>
        private void Disable()
        {
            SandboxDisabled?.Invoke();
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
            _adminManager.AdminStatusUpdated -= CheckStatus;
        }

        private void OnSandboxStatus(MsgSandboxStatus ev)
        {
            SetAllowed(ev.SandboxAllowed);
        }

        private void SetAllowed(bool newAllowed)
        {
            if (newAllowed == SandboxAllowed)
                return;

            SandboxAllowed = newAllowed;
            if (!CanSandbox())
                Disable();
        }

        public void Respawn()
        {
            RaiseNetworkEvent(new MsgSandboxRespawn());
        }

        public void GiveAdminAccess()
        {
            RaiseNetworkEvent(new MsgSandboxGiveAccess());
        }

        public void GiveAGhost()
        {
            RaiseNetworkEvent(new MsgSandboxGiveAghost());
        }

        public void Suicide()
        {
            RaiseNetworkEvent(new MsgSandboxSuicide());
        }

        // TODO: need to cleanup these
        public void ToggleLight()
        {
            _consoleHost.ExecuteCommand("togglelight");
        }

        public void ToggleFov()
        {
            _consoleHost.ExecuteCommand("togglefov");
        }

        public void ToggleShadows()
        {
            _consoleHost.ExecuteCommand("toggleshadows");
        }

        public void ToggleSubFloor()
        {
            _consoleHost.ExecuteCommand("showsubfloor");
        }

        public void ShowMarkers()
        {
            _consoleHost.ExecuteCommand("showmarkers");
        }

        public void ShowBb()
        {
            _consoleHost.ExecuteCommand("physics shapes");
        }

        public void MachineLinking()
        {
            _consoleHost.ExecuteCommand("signallink");
        }
    }
}
