using Content.Server.Singularity.Components;
using Content.Shared.Singularity;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Dynamics;

namespace Content.Server.Singularity.EntitySystems
{
    [UsedImplicitly]
    public class SingularitySystem : SharedSingularitySystem
    {
        private float _updateInterval = 1.0f;
        private float _accumulator;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ServerSingularityComponent, StartCollideEvent>(HandleCollide);
        }

        private void HandleCollide(EntityUid uid, ServerSingularityComponent component, StartCollideEvent args)
        {
            // If we're being deleted by another singularity, this call is probably for that singularity.
            // Even if not, just don't bother.
            if (component.BeingDeletedByAnotherSingularity)
                return;

            var otherEntity = args.OtherFixture.Body.Owner;

            if (otherEntity.TryGetComponent<IMapGridComponent>(out var mapGridComponent))
            {
                foreach (var tile in mapGridComponent.Grid.GetTilesIntersecting(args.OurFixture.Body.GetWorldAABB()))
                {
                    mapGridComponent.Grid.SetTile(tile.GridIndices, Robust.Shared.Map.Tile.Empty);
                    component.Energy++;
                }
                return;
            }

            if (otherEntity.HasComponent<ContainmentFieldComponent>() ||
                (otherEntity.TryGetComponent<ContainmentFieldGeneratorComponent>(out var containmentField) && containmentField.CanRepell(component.Owner)))
            {
                return;
            }

            if (otherEntity.IsInContainer())
                return;

            // Singularity priority management / etc.
            if (otherEntity.TryGetComponent<ServerSingularityComponent>(out var otherSingulo))
                otherSingulo.BeingDeletedByAnotherSingularity = true;

            otherEntity.QueueDelete();

            if (otherEntity.TryGetComponent<SinguloFoodComponent>(out var singuloFood))
                component.Energy += singuloFood.Energy;
            else
                component.Energy++;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            _accumulator += frameTime;

            while (_accumulator > _updateInterval)
            {
                _accumulator -= _updateInterval;

                foreach (var singularity in ComponentManager.EntityQuery<ServerSingularityComponent>())
                {
                    singularity.Update(1);
                }
            }
        }
    }
}
