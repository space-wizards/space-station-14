using Content.Client.Administration.Managers;
using Content.Client.Movement.Systems;
using Content.Shared.Sandbox;
using Robust.Client.Console;
using Robust.Client.Placement;
using Robust.Client.Placement.Modes;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Client.Sandbox
{
    public sealed class SandboxSystem : SharedSandboxSystem
    {
        [Dependency] private readonly IClientAdminManager _adminManager = default!;
        [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
        [Dependency] private readonly IMapManager _map = default!;
        [Dependency] private readonly IPlacementManager _placement = default!;
        [Dependency] private readonly ContentEyeSystem _contentEye = default!;

        private bool _sandboxEnabled;
        public bool SandboxAllowed { get; private set; }
        public event Action? SandboxEnabled;
        public event Action? SandboxDisabled;

        public override void Initialize()
        {
            _adminManager.AdminStatusUpdated += CheckStatus;
            SubscribeNetworkEvent<MsgSandboxStatus>(OnSandboxStatus);
        }

        private void CheckStatus()
        {
            var currentStatus = _sandboxEnabled || _adminManager.IsActive();
            if (currentStatus == SandboxAllowed)
                return;
            SandboxAllowed = currentStatus;
            if (SandboxAllowed)
            {
                SandboxEnabled?.Invoke();
            }
            else
            {
                SandboxDisabled?.Invoke();
            }
        }

        public override void Shutdown()
        {
            _adminManager.AdminStatusUpdated -= CheckStatus;
            base.Shutdown();
        }

        private void OnSandboxStatus(MsgSandboxStatus ev)
        {
            SetAllowed(ev.SandboxAllowed);
        }

        private void SetAllowed(bool sandboxEnabled)
        {
            _sandboxEnabled = sandboxEnabled;
            CheckStatus();
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
            if (!SandboxAllowed)
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
            if (!_map.TryFindGridAt(coords.ToMap(EntityManager), out _, out var grid) || !grid.TryGetTileRef(coords, out var tileRef))
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
            _contentEye.RequestToggleFov();
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
