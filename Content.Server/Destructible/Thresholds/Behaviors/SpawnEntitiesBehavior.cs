using Content.Server.Stack;
using Content.Shared.Prototypes;
using Content.Shared.Random.Helpers;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    [Serializable]
    [DataDefinition]
    public sealed class SpawnEntitiesBehavior : IThresholdBehavior
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
            
            var offsetPosition = () => position.Offset((
                (system.Random.NextFloat() * 2 - 1) * Offset,
                (system.Random.NextFloat() * 2 - 1) * Offset));

            foreach (var (entityId, minMax) in Spawn)
            {
                var count = minMax.Min >= minMax.Max
                    ? minMax.Min
                    : system.Random.Next(minMax.Min, minMax.Max + 1);

                if (count == 0) continue;

                if (EntityPrototypeHelpers.HasComponent<StackComponent>(entityId))
                {
                    var spawned = system.EntityManager.SpawnEntity(entityId, offsetPosition());
                    var stack = system.EntityManager.GetComponent<StackComponent>(spawned);
                    EntitySystem.Get<StackSystem>().SetCount(spawned, count, stack);
                    spawned.RandomOffset(Offset);
                }
                else
                {
                    for (var i = 0; i < count; i++)
                    {
                        var spawned = system.EntityManager.SpawnEntity(entityId, offsetPosition());
                        spawned.RandomOffset(Offset);
                    }
                }
            }
        }
    }
}
