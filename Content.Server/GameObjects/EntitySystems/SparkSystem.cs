using Content.Server.GameObjects.Components.Power;
using Content.Shared.Physics;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using System;
using System.Collections.Generic;

namespace Content.Server.GameObjects.EntitySystems
{
    /// <summary>
    ///     Handles spark entities which are entities and not particles because they require behavior
    ///     and collision.
    ///
    ///     TODO: Maybe add pooling and genericize the system for similar effects.
    /// </summary>
    [UsedImplicitly]
    public sealed class SparkSystem : EntitySystem
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        private readonly List<IEntity> _deleteQueue = new List<IEntity>();

        public override void Update(float frameTime)
        {
            foreach (var comp in ComponentManager.EntityQuery<SparkComponent>())
            {

                if (comp.AccumulatedFrameTime >= comp.Lifetime)
                {
                    _deleteQueue.Add(comp.Owner);
                }

                comp.Update(frameTime);
            }

            foreach (var entity in _deleteQueue)
            {
                entity.Delete();
            }

            _deleteQueue.Clear();
        }

        public void CreateSparks(EntityCoordinates coords, int minAmount, int maxAmount)
        {
            var amount = _random.Next(minAmount, maxAmount);
            for (var i = amount; i > 0; i--)
            {
                var spark = _entityManager.SpawnEntity("SparkEffect", coords);
                spark.GetComponent<SparkComponent>().Lifetime = Math.Min(0.5f, _random.NextFloat());
                if (!spark.TryGetComponent<ICollidableComponent>(out var collidable)) continue;
                collidable.EnsureController<MoverController>()
                    .Push(Angle.FromDegrees(_random.Next(360)).ToVec(), 3.0f);
            }
        }
    }
}
