using Content.Client.GameObjects.Components.Observer;
using Content.Client.UserInterface;
using Content.Shared.GameObjects.EntitySystems;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class GhostSystem : SharedGhostSystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IGameHud _gameHud = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GhostComponent, ComponentInit>(OnGhostInit);
            SubscribeLocalEvent<GhostComponent, ComponentRemove>(OnGhostRemove);

            SubscribeLocalEvent<GhostComponent, PlayerAttachedEvent>(OnGhostPlayerAttach);
            SubscribeLocalEvent<GhostComponent, PlayerDetachedEvent>(OnGhostPlayerDetach);
        }

        private void OnGhostInit(EntityUid uid, GhostComponent component, ComponentInit args)
        {
            if (component.Owner.TryGetComponent(out SpriteComponent? sprite))
            {
                sprite.Visible = _playerManager
                    .LocalPlayer?
                    .ControlledEntity?
                    .HasComponent<GhostComponent>() ?? false;
            }
        }

        private void OnGhostRemove(EntityUid uid, GhostComponent component, ComponentRemove args)
        {
            component.Gui?.Dispose();

            // PlayerDetachedMsg might not fire due to deletion order so...
            if (component.IsAttached)
            {
                SetGhostVisibility(false);
            }
        }

        private void OnGhostPlayerAttach(EntityUid uid, GhostComponent component, PlayerAttachedEvent playerAttachedEvent)
        {
            if (component.Gui == null)
            {
                component.Gui = new GhostGui(component, EntityManager.EntityNetManager!);
                component.Gui.Update();
            }
            else
            {
                component.Gui.Orphan();
            }

            _gameHud.HandsContainer.AddChild(component.Gui);
            SetGhostVisibility(true);
            component.IsAttached = true;
        }

        private void OnGhostPlayerDetach(EntityUid uid, GhostComponent component, PlayerDetachedEvent args)
        {
            component.Gui?.Parent?.RemoveChild(component.Gui);
            SetGhostVisibility(false);
            component.IsAttached = false;
        }

        private void SetGhostVisibility(bool visibility)
        {
            foreach (var ghost in ComponentManager.GetAllComponents(typeof(GhostComponent), true))
            {
                if (ghost.Owner.TryGetComponent(out SpriteComponent? sprite))
                {
                    sprite.Visible = visibility;
                }
            }
        }
    }
}
