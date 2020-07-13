using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Map;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Conveyor
{
    // TODO: Start/stop, directional textures
    [RegisterComponent]
    public class ConveyorComponent : Component
    {
#pragma warning disable 649
        [Dependency] private readonly IEntityManager _entityManager;
#pragma warning restore 649

        public override string Name => "Conveyor";

        /// <summary>
        ///     The angle in radians to move entities by in relation
        ///     to the owner's rotation.
        ///     Parsed from YAML as degrees.
        /// </summary>
        private double _angle;

        /// <summary>
        ///     The amount of units to move the entity by.
        /// </summary>
        private float _speed;

        private Angle GetAngle()
        {
            return new Angle(Owner.Transform.LocalRotation.Theta + _angle);
        }

        public void Update(float frameTime)
        {
            var intersecting = _entityManager.GetEntitiesIntersecting(Owner);

            foreach (var entity in intersecting)
            {
                if (entity == Owner ||
                    entity.TryGetComponent(out PhysicsComponent physics) &&
                    physics.Anchored ||
                    entity.HasComponent<IMapGridComponent>())
                {
                    continue;
                }

                entity.Transform.WorldPosition += GetAngle().ToVec() * _speed * frameTime;
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            double degrees = 0;
            serializer.DataField(ref degrees, "angle", 0);

            _angle = MathHelper.DegreesToRadians(degrees);

            serializer.DataField(ref _speed, "speed", 2);
        }
    }
}
