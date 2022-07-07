using Content.Client.Administration.Managers;
using Content.Shared.GameTicking;
using Content.Shared.Sandbox;
using Robust.Client.Console;
using Robust.Client.Placement;
using Robust.Client.Placement.Modes;
using Robust.Shared.Map;
using Robust.Shared.Players;

namespace Content.Client.Sandbox
{
    public sealed class SandboxSystem : SharedSandboxSystem
    {
        [Dependency] private readonly IClientAdminManager _adminManager = default!;
        [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
        [Dependency] private readonly IMapManager _map = default!;
        [Dependency] private readonly IPlacementManager _placement = default!;

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

        public bool Copy(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            if (!CanSandbox())
                return false;

            // Try copy entity.
            if (uid.IsValid()
                && EntityManager.TryGetComponent(uid, out MetaDataComponent? comp)
                && !comp.EntityDeleted)
            {
                if (comp.EntityPrototype == null || comp.EntityPrototype.NoSpawn || comp.EntityPrototype.Abstract)
                    return false;

                if (_placement.Eraser)
                    _placement.ToggleEraser();

                _placement.BeginPlacing(new()
                {
                    EntityType = comp.EntityPrototype.ID,
                    IsTile = false,
                    TileType = 0,
                    PlacementOption = comp.EntityPrototype.PlacementMode
                });
                return true;
            }

            // Try copy tile.
            if (!_map.TryFindGridAt(coords.ToMap(EntityManager), out var grid) || !grid.TryGetTileRef(coords, out var tileRef))
                return false;

            if (_placement.Eraser)
                _placement.ToggleEraser();

            _placement.BeginPlacing(new()
            {
                EntityType = null,
                IsTile = true,
                TileType = tileRef.Tile.TypeId,
                PlacementOption = nameof(AlignTileAny)
            });
            return true;
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
