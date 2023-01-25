using Content.Server.Stack;
using Content.Shared.Prototypes;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    [Serializable]
    [DataDefinition]
    public sealed class SpawnEntitiesBehavior : IThresholdBehavior
    {
        /// <summary>
        ///     Entities spawned on reaching this threshold, from a min to a max.
        /// </summary>
        [DataField("spawn", customTypeSerializer:typeof(PrototypeIdDictionarySerializer<MinMax, EntityPrototype>))]
        public Dictionary<string, MinMax> Spawn { get; set; } = new();

        [DataField("offset")]
        public float Offset { get; set; } = 0.5f;

        public void Execute(EntityUid owner, DestructibleSystem system)
        {
            var position = system.EntityManager.GetComponent<TransformComponent>(owner).MapPosition;

            var getRandomVector = () => new Vector2(system.Random.NextFloat(-Offset, Offset), system.Random.NextFloat(-Offset, Offset));

            foreach (var (entityId, minMax) in Spawn)
            {
                var count = minMax.Min >= minMax.Max
                    ? minMax.Min
                    : system.Random.Next(minMax.Min, minMax.Max + 1);

                if (count == 0) continue;

                if (EntityPrototypeHelpers.HasComponent<StackComponent>(entityId, system.PrototypeManager, system.ComponentFactory))
                {
                    var spawned = system.EntityManager.SpawnEntity(entityId, position.Offset(getRandomVector()));
                    system.StackSystem.SetCount(spawned, count);
                }
                else
                {
                    for (var i = 0; i < count; i++)
                    {
                        system.EntityManager.SpawnEntity(entityId, position.Offset(getRandomVector()));
                    }
                }
            }
        }
    }
}
