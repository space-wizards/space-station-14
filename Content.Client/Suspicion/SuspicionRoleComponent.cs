using Content.Shared.Suspicion;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using static Robust.Client.UserInterface.Controls.LayoutContainer;

namespace Content.Client.Suspicion
{
    [RegisterComponent]
    public sealed class SuspicionRoleComponent : SharedSuspicionRoleComponent
    {
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IUserInterfaceManager _ui = default!;

        private SuspicionGui? _gui;
        private string? _role;
        private bool? _antagonist;
        private bool _overlayActive;

        public string? Role
        {
            get => _role;
            set
            {
                if (_role == value)
                {
                    return;
                }

                _role = value;
                _gui?.UpdateLabel();
                Dirty();
            }
        }

        public bool? Antagonist
        {
            get => _antagonist;
            set
            {
                if (_antagonist == value)
                {
                    return;
                }

                _antagonist = value;
                _gui?.UpdateLabel();

                if (value ?? false)
                {
                    AddTraitorOverlay();
                }

                Dirty();
            }
        }

        [ViewVariables]
        public List<(string name, EntityUid uid)> Allies { get; } = new();

        private void AddTraitorOverlay()
        {
            if (_overlayManager.HasOverlay<TraitorOverlay>())
            {
                return;
            }

            _overlayActive = true;
            var entManager = IoCManager.Resolve<IEntityManager>();
            var overlay = new TraitorOverlay(entManager, IoCManager.Resolve<IPlayerManager>(), _resourceCache, entManager.System<EntityLookupSystem>());
            _overlayManager.AddOverlay(overlay);
        }

        private void RemoveTraitorOverlay()
        {
            if (!_overlayActive)
            {
                return;
            }

            _overlayManager.RemoveOverlay<TraitorOverlay>();
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not SuspicionRoleComponentState state)
            {
                return;
            }

            Role = state.Role;
            Antagonist = state.Antagonist;
            Allies.Clear();
            Allies.AddRange(state.Allies);
        }

        public void RemoveUI()
        {
            _gui?.Parent?.RemoveChild(_gui);
            RemoveTraitorOverlay();
        }

        public void AddUI()
        {
            // TODO move this out of the component
            _gui = _ui.ActiveScreen?.GetOrNewWidget<SuspicionGui>();
            _gui!.UpdateLabel();
            SetAnchorAndMarginPreset(_gui, LayoutPreset.BottomLeft);

            if (_antagonist ?? false)
            {
                AddTraitorOverlay();
            }
        }

        protected override void OnRemove()
        {
            base.OnRemove();

            _gui?.Dispose();
            RemoveTraitorOverlay();
        }
    }
}
