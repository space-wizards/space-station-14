using System;
using Content.Shared.Radiation;
using Content.Server.Radiation;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Content.Server.Supermatter;
using Content.Shared.Body.Components;
using Content.Server.Ghost.Components;
using Content.Server.Singularity.Components;
using Content.Shared.Singularity;
using Content.Shared.Singularity.Components;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Content.Server.Body;
using Content.Shared.SubFloor;
using Content.Server.Doors.Components;
using Content.Shared.Doors;
using Content.Shared.Damage;
using Content.Shared.Item;

namespace Content.Server.SupermatterSystem
{
    [UsedImplicitly]

    public class SupermatterSystem : EntitySystem
    {
        [Dependency] private readonly IEntityLookup _lookup = default!;

        //private const float RadiationCooldown = 0.5f;
        //private float _accumulator;
        public override void Initialize()
        {
            base.Initialize();
        }
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var supermatter in EntityManager.EntityQuery<SupermatterComponent>())
                {
                    Update(supermatter, frameTime);
                }
        }
        public bool CanDestroy(SupermatterComponent component, IEntity entity)
        {
            return entity.HasComponent<SharedBodyComponent>() || entity.HasComponent<SharedItemComponent>();
        }
        public void HandleDestroy(SupermatterComponent component, IEntity entity)
        {
            // TODO: Need SM immune tag
            if (!CanDestroy(component, entity)) return;

            entity.QueueDelete();

            if (entity.TryGetComponent<SinguloFoodComponent>(out var singuloFood))
                component.Energy += singuloFood.Energy;
            else
                component.Energy++;
        }

        /// <summary>
        /// Handle deleting entities and increasing energy
        /// </summary>
        public void DestroyEntities(SupermatterComponent component, Vector2 worldPos)
        {
            foreach (var entity in _lookup.GetEntitiesInRange(component.Owner.Transform.MapID, worldPos, 0.5f)) //GetEntitiesIntersecting(component.Owner.Transform.MapID, worldPos)
            {
                HandleDestroy(component, entity);
            }
        }
        public void Update(SupermatterComponent component, float frameTime)
        {

            var worldPos = component.Owner.Transform.WorldPosition;
            DestroyEntities(component, worldPos);
        }
    }
}
