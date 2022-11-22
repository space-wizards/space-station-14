using Content.Shared.Actions;
using Content.Shared.Ghost;
using JetBrains.Annotations;
using Robust.Client.Console;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.GameStates;

namespace Content.Client.Ghost
{
    [UsedImplicitly]
    public sealed class GhostSystem : SharedGhostSystem
    {
        [Dependency] private readonly IClientConsoleHost _console = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly ILightManager _lightManager = default!;

        public int AvailableGhostRoleCount { get; private set; }

        private bool _ghostVisibility = true;

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

                foreach (var ghost in EntityQuery<GhostComponent, SpriteComponent>(true))
                {
                    ghost.Item2.Visible = true;
                }
            }
        }

        public GhostComponent? Player => CompOrNull<GhostComponent>(_playerManager.LocalPlayer?.ControlledEntity);
        public bool IsGhost => Player != null;

        public event Action<GhostComponent>? PlayerRemoved;
        public event Action<GhostComponent>? PlayerUpdated;
        public event Action<GhostComponent>? PlayerAttached;
        public event Action? PlayerDetached;
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

            SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttach);

            SubscribeNetworkEvent<GhostWarpsResponseEvent>(OnGhostWarpsResponse);
            SubscribeNetworkEvent<GhostUpdateGhostRoleCountEvent>(OnUpdateGhostRoleCount);

            SubscribeLocalEvent<GhostComponent, DisableLightingActionEvent>(OnActionPerform);
            SubscribeLocalEvent<GhostComponent, ToggleGhostsActionEvent>(OnToggleGhosts);
        }

        private void OnGhostInit(EntityUid uid, GhostComponent component, ComponentInit args)
        {
            if (TryComp(component.Owner, out SpriteComponent? sprite))
            {
                sprite.Visible = GhostVisibility;
            }

            _actions.AddAction(uid, component.DisableLightingAction, null);
            _actions.AddAction(uid, component.ToggleGhostsAction, null);
        }

        private void OnActionPerform(EntityUid uid, GhostComponent component, DisableLightingActionEvent args)
        {
            if (args.Handled)
                return;

            _lightManager.Enabled = !_lightManager.Enabled;
            args.Handled = true;
        }

        private void OnToggleGhosts(EntityUid uid, GhostComponent component, ToggleGhostsActionEvent args)
        {
            if (args.Handled)
                return;

            ToggleGhostVisibility();
            args.Handled = true;
        }

        private void OnGhostRemove(EntityUid uid, GhostComponent component, ComponentRemove args)
        {
            _actions.RemoveAction(uid, component.DisableLightingAction);
            _actions.RemoveAction(uid, component.ToggleGhostsAction);
            _lightManager.Enabled = true;

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

        private bool PlayerDetach(EntityUid uid)
        {
            if (uid != _playerManager.LocalPlayer?.ControlledEntity)
                return false;

            GhostVisibility = false;
            PlayerDetached?.Invoke();
            return true;
        }

        private void OnGhostPlayerDetach(EntityUid uid, GhostComponent component, PlayerDetachedEvent args)
        {
            if (PlayerDetach(uid))
                component.IsAttached = false;
        }

        private void OnPlayerAttach(PlayerAttachedEvent ev)
        {
            if (!HasComp<GhostComponent>(ev.Entity))
                PlayerDetach(ev.Entity);
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

        public void ToggleGhostVisibility()
        {
            _console.RemoteExecuteCommand(null, "toggleghosts");
        }
    }
}
