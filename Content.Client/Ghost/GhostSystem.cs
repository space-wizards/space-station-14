using Content.Shared.Ghost;
using JetBrains.Annotations;
using Robust.Client.Console;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.GameStates;

namespace Content.Client.Ghost
{
    [UsedImplicitly]
    public sealed class GhostSystem : SharedGhostSystem
    {
        [Dependency] private readonly IClientConsoleHost _console = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public int AvailableGhostRoleCount { get; private set; }

        private bool _ghostVisibility;

        private bool GhostVisibility
        {
            get => _ghostVisibility;
            set
            {
                if (_ghostVisibility == value)
                {
                    return;
                }

                _ghostVisibility = value;

                foreach (var ghost in EntityManager.GetAllComponents(typeof(GhostComponent), true))
                {
                    if (TryComp(ghost.Owner, out SpriteComponent? sprite))
                    {
                        sprite.Visible = value;
                    }
                }
            }
        }

        public GhostComponent? Player => CompOrNull<GhostComponent>(_playerManager.LocalPlayer?.ControlledEntity);
        public bool IsGhost => Player != null;

        public event Action<GhostComponent>? PlayerRemoved;
        public event Action<GhostComponent>? PlayerUpdated;
        public event Action<GhostComponent>? PlayerAttached;
        public event Action<GhostComponent>? PlayerDetached;
        public event Action<GhostWarpsResponseEvent>? GhostWarpsResponse;
        public event Action<GhostUpdateGhostRoleCountEvent>? GhostRoleCountUpdated;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GhostComponent, ComponentInit>(OnGhostInit);
            SubscribeLocalEvent<GhostComponent, ComponentRemove>(OnGhostRemove);
            SubscribeLocalEvent<GhostComponent, ComponentHandleState>(OnGhostState);

            SubscribeLocalEvent<GhostComponent, PlayerAttachedEvent>(OnGhostPlayerAttach);
            SubscribeLocalEvent<GhostComponent, PlayerDetachedEvent>(OnGhostPlayerDetach);

            SubscribeNetworkEvent<GhostWarpsResponseEvent>(OnGhostWarpsResponse);
            SubscribeNetworkEvent<GhostUpdateGhostRoleCountEvent>(OnUpdateGhostRoleCount);
        }

        private void OnGhostInit(EntityUid uid, GhostComponent component, ComponentInit args)
        {
            if (TryComp(component.Owner, out SpriteComponent? sprite))
            {
                sprite.Visible = GhostVisibility;
            }
        }

        private void OnGhostRemove(EntityUid uid, GhostComponent component, ComponentRemove args)
        {
            if (uid != _playerManager.LocalPlayer?.ControlledEntity)
                return;

            if (component.IsAttached)
            {
                GhostVisibility = false;
            }

            PlayerRemoved?.Invoke(component);
        }

        private void OnGhostPlayerAttach(EntityUid uid, GhostComponent component, PlayerAttachedEvent playerAttachedEvent)
        {
            if (uid != _playerManager.LocalPlayer?.ControlledEntity)
                return;

            GhostVisibility = true;
            component.IsAttached = true;
            PlayerAttached?.Invoke(component);
        }

        private void OnGhostState(EntityUid uid, GhostComponent component, ref ComponentHandleState args)
        {
            if (uid != _playerManager.LocalPlayer?.ControlledEntity)
                return;

            PlayerUpdated?.Invoke(component);
        }

        private void OnGhostPlayerDetach(EntityUid uid, GhostComponent component, PlayerDetachedEvent args)
        {
            if (uid != _playerManager.LocalPlayer?.ControlledEntity)
                return;

            GhostVisibility = false;
            component.IsAttached = false;
            PlayerDetached?.Invoke(component);
        }

        private void OnGhostWarpsResponse(GhostWarpsResponseEvent msg)
        {
            if (!IsGhost)
            {
                return;
            }

            GhostWarpsResponse?.Invoke(msg);
        }

        private void OnUpdateGhostRoleCount(GhostUpdateGhostRoleCountEvent msg)
        {
            AvailableGhostRoleCount = msg.AvailableGhostRoles;
            GhostRoleCountUpdated?.Invoke(msg);
        }

        public void RequestWarps()
        {
            RaiseNetworkEvent(new GhostWarpsRequestEvent());
        }

        public void ReturnToBody()
        {
            var msg = new GhostReturnToBodyRequest();
            RaiseNetworkEvent(msg);
        }

        public void OpenGhostRoles()
        {
            _console.RemoteExecuteCommand(null, "ghostroles");
        }
    }
}
