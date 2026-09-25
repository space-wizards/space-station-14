using Content.Client.Movement.Systems;
using Content.Shared.Actions;
using Content.Shared.Ghost.Components;
using Content.Shared.Ghost.Systems;
using Content.Shared.NightVision;
using Content.Shared.Overlays;
using Robust.Client.Console;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.Ghost
{
    public sealed partial class GhostSystem : SharedGhostSystem
    {
        [Dependency] private IClientConsoleHost _console = default!;
        [Dependency] private IPlayerManager _playerManager = default!;
        [Dependency] private SharedActionsSystem _actions = default!;
        [Dependency] private ContentEyeSystem _contentEye = default!;
        [Dependency] private SpriteSystem _sprite = default!;
        [Dependency] private SharedNightVisionSystem _nv = default!;

        [Dependency] private EntityQuery<SpriteComponent> _spriteQuery;
        [Dependency] private EntityQuery<NightVisionComponent> _nightVisionQuery;

        public int AvailableGhostRoleCount { get; private set; }

        public GhostVisibilityMode GhostVisibility { get; private set; } = GhostVisibilityMode.ShowAllGhosts;

        public GhostComponent? Player => CompOrNull<GhostComponent>(_playerManager.LocalEntity);
        public bool IsGhost => Player != null;

        public event Action<GhostComponent>? PlayerRemoved;
        public event Action<GhostComponent>? PlayerUpdated;
        public event Action<GhostComponent>? PlayerAttached;
        public event Action? PlayerDetached;
        public event Action<GhostWarpsResponseEvent>? GhostWarpsResponse;
        public event Action<GhostUpdateGhostRoleCountEvent>? GhostRoleCountUpdated;

        [SubscribeLocalEvent]
        private void OnStartup(EntityUid uid, GhostComponent component, ComponentStartup args)
        {
            if (!_spriteQuery.TryComp(uid, out var sprite))
                return;

            _sprite.SetVisible((uid, sprite), GetGhostVisible(uid, GhostVisibility));
        }

        [SubscribeLocalEvent]
        private void OnToggleLighting(EntityUid uid, EyeComponent component, ToggleLightingActionEvent args)
        {
            if (args.Handled)
                return;

            if (!component.DrawLight)
            {
                // normal lighting
                Popup.PopupEntity(Loc.GetString("ghost-gui-toggle-lighting-manager-popup-normal"), args.Performer);
                _contentEye.RequestEye(component.DrawFov, true);
            }
            else if (_nightVisionQuery.TryComp(uid, out var nv) && !nv.Enabled)
            {
                Popup.PopupEntity(Loc.GetString("ghost-gui-toggle-lighting-manager-popup-half-bright"), args.Performer);
                _nv.SetEnabled((uid, nv), true);
            }
            else
            {
                // fullbright mode
                Popup.PopupEntity(Loc.GetString("ghost-gui-toggle-lighting-manager-popup-fullbright"), args.Performer);
                _contentEye.RequestEye(component.DrawFov, false);
                _nv.SetEnabled((uid, nv), false);
            }

            args.Handled = true;
        }

        [SubscribeLocalEvent]
        private void OnToggleFoV(EntityUid uid, EyeComponent component, ToggleFoVActionEvent args)
        {
            if (args.Handled)
                return;

            Popup.PopupEntity(Loc.GetString("ghost-gui-toggle-fov-popup"), args.Performer);
            _contentEye.RequestToggleFov(uid, component);
            args.Handled = true;
        }

        [SubscribeLocalEvent]
        private void OnToggleGhosts(EntityUid uid, GhostComponent component, ToggleGhostsActionEvent args)
        {
            if (args.Handled)
                return;

            var locId = GhostVisibility switch
            {
                GhostVisibilityMode.ShowAllGhosts => "ghost-gui-toggle-ghost-visibility-popup-off",
                GhostVisibilityMode.HideOtherGhosts => "ghost-gui-toggle-all-ghosts-visibility-popup-off",
                GhostVisibilityMode.HideOtherGhostsAndSelf => "ghost-gui-toggle-ghost-visibility-popup-on",
                _ => throw new ArgumentOutOfRangeException()
            };

            Popup.PopupEntity(Loc.GetString(locId), args.Performer);
            if (uid == _playerManager.LocalEntity)
                ToggleGhostVisibility();

            args.Handled = true;
        }

        [SubscribeLocalEvent]
        private void OnGhostRemove(EntityUid uid, GhostComponent component, ComponentRemove args)
        {
            _actions.RemoveAction(uid, component.ToggleLightingActionEntity);
            _actions.RemoveAction(uid, component.ToggleFoVActionEntity);
            _actions.RemoveAction(uid, component.ToggleGhostsActionEntity);
            _actions.RemoveAction(uid, component.ToggleGhostHearingActionEntity);

            if (uid != _playerManager.LocalEntity)
                return;

            ApplyGhostVisibility(GhostVisibilityMode.HideOtherGhosts);
            PlayerRemoved?.Invoke(component);
        }

        [SubscribeLocalEvent]
        private void OnGhostPlayerAttach(EntityUid uid, GhostComponent component, LocalPlayerAttachedEvent localPlayerAttachedEvent)
        {
            ApplyGhostVisibility(GhostVisibilityMode.ShowAllGhosts);
            PlayerAttached?.Invoke(component);
        }

        [SubscribeLocalEvent]
        private void OnGhostState(EntityUid uid, GhostComponent component, ref AfterAutoHandleStateEvent args)
        {
            if (!_spriteQuery.TryComp(uid, out var sprite))
                _sprite.LayerSetColor((uid, sprite), 0, component.Color);

            if (uid != _playerManager.LocalEntity)
                return;

            PlayerUpdated?.Invoke(component);
        }

        [SubscribeLocalEvent]
        private void OnGhostPlayerDetach(EntityUid uid, GhostComponent component, LocalPlayerDetachedEvent args)
        {
            ApplyGhostVisibility(GhostVisibilityMode.HideOtherGhosts);
            PlayerDetached?.Invoke();
        }

        [SubscribeNetworkEvent]
        private void OnGhostWarpsResponse(GhostWarpsResponseEvent msg)
        {
            if (!IsGhost)
            {
                return;
            }

            GhostWarpsResponse?.Invoke(msg);
        }

        [SubscribeNetworkEvent]
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

        /// <summary>
        /// Advances through the available ghost visibility modes in sequence.
        /// </summary>
        public void ToggleGhostVisibility()
        {
            var nextMode = GhostVisibility switch
            {
                GhostVisibilityMode.ShowAllGhosts => GhostVisibilityMode.HideOtherGhosts,
                GhostVisibilityMode.HideOtherGhosts => GhostVisibilityMode.HideOtherGhostsAndSelf,
                GhostVisibilityMode.HideOtherGhostsAndSelf => GhostVisibilityMode.ShowAllGhosts,
                _ => throw new ArgumentOutOfRangeException()
            };

            ApplyGhostVisibility(nextMode);
        }

        private void ApplyGhostVisibility(GhostVisibilityMode mode)
        {
            if (GhostVisibility == mode)
                return;

            GhostVisibility = mode;

            var query = AllEntityQuery<GhostComponent, SpriteComponent>();
            while (query.MoveNext(out var uid, out _, out var sprite))
            {
                _sprite.SetVisible((uid, sprite), GetGhostVisible(uid, mode));
            }
        }

        private bool GetGhostVisible(EntityUid uid, GhostVisibilityMode mode)
        {
            return mode switch
            {
                GhostVisibilityMode.ShowAllGhosts => true,
                GhostVisibilityMode.HideOtherGhosts => uid == _playerManager.LocalEntity,
                GhostVisibilityMode.HideOtherGhostsAndSelf => false,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

    }

    /// <summary>
    /// Controls which ghost sprites are visible to the local client.
    /// </summary>
    public enum GhostVisibilityMode : byte
    {
        /// <summary>
        /// Show all ghosts.
        /// </summary>
        ShowAllGhosts,

        /// <summary>
        /// Hide other ghosts, but keep the local ghost visible.
        /// </summary>
        HideOtherGhosts,

        /// <summary>
        /// Hide all ghosts, including the local ghost.
        /// </summary>
        HideOtherGhostsAndSelf,
    }
}
