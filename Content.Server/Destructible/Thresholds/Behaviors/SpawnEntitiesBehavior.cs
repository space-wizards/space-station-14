using System.Numerics;
using Content.Server.Forensics;
using Content.Server.Stack;
using Content.Shared.Prototypes;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    [Serializable]
    [DataDefinition]
    public sealed partial class SpawnEntitiesBehavior : IThresholdBehavior
    {
        /// <summary>
        ///     Entities spawned on reaching this threshold, from a min to a max.
        /// </summary>
        [DataField("spawn", customTypeSerializer:typeof(PrototypeIdDictionarySerializer<MinMax, EntityPrototype>))]
        public Dictionary<string, MinMax> Spawn { get; set; } = new();

        [DataField("offset")]
        public float Offset { get; set; } = 0.5f;

        [DataField("transferForensics")]
        public bool DoTransferForensics = false;

        public void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null)
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

                    TransferForensics(spawned, system, owner);
                }
                else
                {
                    for (var i = 0; i < count; i++)
                    {
                        var spawned = system.EntityManager.SpawnEntity(entityId, position.Offset(getRandomVector()));

                        TransferForensics(spawned, system, owner);
                    }
                }
            }
        }

        public void TransferForensics(EntityUid spawned, DestructibleSystem system, EntityUid owner)
        {
            if (!DoTransferForensics ||
                !system.EntityManager.TryGetComponent<ForensicsComponent>(owner, out var forensicsComponent))
                return;

            var comp = system.EntityManager.EnsureComponent<ForensicsComponent>(spawned);
            comp.DNAs = forensicsComponent.DNAs;

            if (!system.Random.Prob(0.4f))
                return;
            comp.Fingerprints = forensicsComponent.Fingerprints;
            comp.Fibers = forensicsComponent.Fibers;
        }
    }
}
