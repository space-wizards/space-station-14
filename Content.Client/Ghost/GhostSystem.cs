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

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GhostComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<GhostComponent, ComponentRemove>(OnGhostRemove);
            SubscribeLocalEvent<GhostComponent, AfterAutoHandleStateEvent>(OnGhostState);

            SubscribeLocalEvent<GhostComponent, LocalPlayerAttachedEvent>(OnGhostPlayerAttach);
            SubscribeLocalEvent<GhostComponent, LocalPlayerDetachedEvent>(OnGhostPlayerDetach);

            SubscribeNetworkEvent<GhostWarpsResponseEvent>(OnGhostWarpsResponse);
            SubscribeNetworkEvent<GhostUpdateGhostRoleCountEvent>(OnUpdateGhostRoleCount);

            SubscribeLocalEvent<EyeComponent, ToggleLightingActionEvent>(OnToggleLighting);
            SubscribeLocalEvent<EyeComponent, ToggleFoVActionEvent>(OnToggleFoV);
            SubscribeLocalEvent<GhostComponent, ToggleGhostsActionEvent>(OnToggleGhosts);
        }

        private void OnStartup(EntityUid uid, GhostComponent component, ComponentStartup args)
        {
            if (!TryComp(uid, out SpriteComponent? sprite))
                return;

            _sprite.SetVisible((uid, sprite), GetGhostVisible(uid, GhostVisibility));
        }

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
            else if (TryComp<NightVisionComponent>(uid, out var nv) && !nv.Enabled)
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

        private void OnToggleFoV(EntityUid uid, EyeComponent component, ToggleFoVActionEvent args)
        {
            if (args.Handled)
                return;

            Popup.PopupEntity(Loc.GetString("ghost-gui-toggle-fov-popup"), args.Performer);
            _contentEye.RequestToggleFov(uid, component);
            args.Handled = true;
        }

        private void OnToggleGhosts(EntityUid uid, GhostComponent component, ToggleGhostsActionEvent args)
        {
            if (args.Handled)
                return;

            var locId = string.Empty;

            switch (GhostVisibility)
            {
                case GhostVisibilityMode.ShowAllGhosts:
                    locId = "ghost-gui-toggle-ghost-visibility-popup-off";
                    break;
                case GhostVisibilityMode.HideOtherGhosts:
                    locId = "ghost-gui-toggle-all-ghosts-visibility-popup-off";
                    break;
                case GhostVisibilityMode.HideOtherGhostsAndSelf:
                    locId = "ghost-gui-toggle-ghost-visibility-popup-on";
                    break;
            }

            Popup.PopupEntity(Loc.GetString(locId), args.Performer);
            if (uid == _playerManager.LocalEntity)
                ToggleGhostVisibility();

            args.Handled = true;
        }

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

        private void OnGhostPlayerAttach(EntityUid uid, GhostComponent component, LocalPlayerAttachedEvent localPlayerAttachedEvent)
        {
            ApplyGhostVisibility(GhostVisibilityMode.ShowAllGhosts);
            PlayerAttached?.Invoke(component);
        }

        private void OnGhostState(EntityUid uid, GhostComponent component, ref AfterAutoHandleStateEvent args)
        {
            if (TryComp<SpriteComponent>(uid, out var sprite))
                _sprite.LayerSetColor((uid, sprite), 0, component.Color);

            if (uid != _playerManager.LocalEntity)
                return;

            PlayerUpdated?.Invoke(component);
        }

        private void OnGhostPlayerDetach(EntityUid uid, GhostComponent component, LocalPlayerDetachedEvent args)
        {
            ApplyGhostVisibility(GhostVisibilityMode.HideOtherGhosts);
            PlayerDetached?.Invoke();
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
            // difficult ass implementation for toggling Enum ghost visibility cyclically
            // (after 1 is 2, after 2 is 3, and after 3 is 1 again)
            // is needed in case somebody would want to add another mode to GhostVisibilityMode Enum so it won't break
            var count = Enum.GetValues(typeof(GhostVisibilityMode)).Length;
            ApplyGhostVisibility((GhostVisibilityMode)(((int)GhostVisibility + 1) % count));
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

    public enum GhostVisibilityMode : byte
    {
        ShowAllGhosts,
        HideOtherGhosts,
        HideOtherGhostsAndSelf,
    }
}
