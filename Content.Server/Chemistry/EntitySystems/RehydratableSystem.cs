using Content.Server.Chemistry.Components;
using Content.Server.Popups;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;

namespace Content.Server.Chemistry.EntitySystems
{
    [UsedImplicitly]
    public sealed class RehydratableSystem : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionsSystem = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RehydratableComponent, SolutionChangedEvent>(OnSolutionChange);
        }

        private void OnSolutionChange(EntityUid uid, RehydratableComponent component, SolutionChangedEvent args)
        {
            if (_solutionsSystem.GetReagentQuantity(uid, component.CatalystPrototype) > FixedPoint2.Zero)
            {
                Expand(component, component.Owner);
            }
        }

        // Try not to make this public if you can help it.
        private void Expand(RehydratableComponent component, EntityUid owner)
        {
            if (component.Expanding)
            {
                return;
            }

            component.Expanding = true;
            owner.PopupMessageEveryone(Loc.GetString("rehydratable-component-expands-message", ("owner", owner)));
            if (!string.IsNullOrEmpty(component.TargetPrototype))
            {
                var ent = EntityManager.SpawnEntity(component.TargetPrototype,
                    EntityManager.GetComponent<TransformComponent>(owner).Coordinates);
                EntityManager.GetComponent<TransformComponent>(ent).AttachToGridOrMap();
            }

            EntityManager.QueueDeleteEntity((EntityUid) owner);
        }
    }
}
