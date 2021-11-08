using Content.Client.Ghost.UI;
using Content.Client.HUD;
using Content.Shared.Ghost;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Ghost
{
    [UsedImplicitly]
    public class GhostSystem : SharedGhostSystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IGameHud _gameHud = default!;

        // Changes to this value are manually propagated.
        // No good way to get an event into the UI.
        public int AvailableGhostRoleCount { get; private set; } = 0;

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
                    if (ghost.Owner.TryGetComponent(out SpriteComponent? sprite))
                    {
                        sprite.Visible = value;
                    }
                }
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GhostComponent, ComponentInit>(OnGhostInit);
            SubscribeLocalEvent<GhostComponent, ComponentRemove>(OnGhostRemove);

            SubscribeLocalEvent<GhostComponent, PlayerAttachedEvent>(OnGhostPlayerAttach);
            SubscribeLocalEvent<GhostComponent, PlayerDetachedEvent>(OnGhostPlayerDetach);

            SubscribeNetworkEvent<GhostWarpsResponseEvent>(OnGhostWarpsResponse);
            SubscribeNetworkEvent<GhostUpdateGhostRoleCountEvent>(OnUpdateGhostRoleCount);
        }

        private void OnGhostInit(EntityUid uid, GhostComponent component, ComponentInit args)
        {
            if (component.Owner.TryGetComponent(out SpriteComponent? sprite))
            {
                sprite.Visible = GhostVisibility;
            }
        }

        private void OnGhostRemove(EntityUid uid, GhostComponent component, ComponentRemove args)
        {
            component.Gui?.Dispose();
            component.Gui = null;

            // PlayerDetachedMsg might not fire due to deletion order so...
            if (component.IsAttached)
            {
                GhostVisibility = false;
            }
        }

        private void OnGhostPlayerAttach(EntityUid uid, GhostComponent component, PlayerAttachedEvent playerAttachedEvent)
        {
            // I hate UI I hate UI I Hate UI
            if (component.Gui == null)
            {
                component.Gui = new GhostGui(component, this, EntityManager.EntityNetManager!);
                component.Gui.Update();
            }

            _gameHud.HandsContainer.AddChild(component.Gui);
            GhostVisibility = true;
            component.IsAttached = true;
        }

        private void OnGhostPlayerDetach(EntityUid uid, GhostComponent component, PlayerDetachedEvent args)
        {
            component.Gui?.Parent?.RemoveChild(component.Gui);
            GhostVisibility = false;
            component.IsAttached = false;
        }

        private void OnGhostWarpsResponse(GhostWarpsResponseEvent msg)
        {
            var entity = _playerManager.LocalPlayer?.ControlledEntity;

            if (entity == null ||
                !entity.TryGetComponent(out GhostComponent? ghost))
            {
                return;
            }

            var window = ghost.Gui?.TargetWindow;

            if (window != null)
            {
                window.Locations = msg.Locations;
                window.Players = msg.Players;
                window.Populate();
            }
        }

        private void OnUpdateGhostRoleCount(GhostUpdateGhostRoleCountEvent msg)
        {
            AvailableGhostRoleCount = msg.AvailableGhostRoles;
            foreach (var ghost in EntityManager.EntityQuery<GhostComponent>(true))
                ghost.Gui?.Update();
        }
    }
}
