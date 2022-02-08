using System;
using System.Collections.Generic;
using Content.Server.Stack;
using Content.Shared.Prototypes;
using Content.Shared.Random.Helpers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    [Serializable]
    [DataDefinition]
    public class SpawnEntitiesBehavior : IThresholdBehavior
    {
        /// <summary>
        ///     Entities spawned on reaching this threshold, from a min to a max.
        /// </summary>
        [DataField("spawn")]
        public Dictionary<string, MinMax> Spawn { get; set; } = new();

        [DataField("offset")]
        public float Offset { get; set; } = 0.5f;

        public void Execute(EntityUid owner, DestructibleSystem system)
        {
            var position = system.EntityManager.GetComponent<TransformComponent>(owner).MapPosition;

            foreach (var (entityId, minMax) in Spawn)
            {
                var count = minMax.Min >= minMax.Max
                    ? minMax.Min
                    : system.Random.Next(minMax.Min, minMax.Max + 1);

                if (count == 0) continue;

                if (EntityPrototypeHelpers.HasComponent<StackComponent>(entityId))
                {
                    var spawned = system.EntityManager.SpawnEntity(entityId, position);
                    var stack = IoCManager.Resolve<IEntityManager>().GetComponent<StackComponent>(spawned);
                    EntitySystem.Get<StackSystem>().SetCount(spawned, count, stack);
                    spawned.RandomOffset(Offset);
                }
                else
                {
                    for (var i = 0; i < count; i++)
                    {
                        var spawned = system.EntityManager.SpawnEntity(entityId, position);
                        spawned.RandomOffset(Offset);
                    }
                }
            }
        }
    }
}
