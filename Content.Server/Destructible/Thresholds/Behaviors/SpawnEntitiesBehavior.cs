using System.Numerics;
using Content.Server.Forensics;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.Destructible.Thresholds.Behaviors;
using Content.Shared.Prototypes;
using Content.Shared.Stacks;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    [Serializable]
    [DataDefinition]
    public sealed partial class SpawnEntitiesBehavior : IThresholdBehavior
    {
        /// <summary>
        ///     Entities spawned on reaching this threshold, from a min to a max.
        /// </summary>
        [DataField]
        public Dictionary<EntProtoId, MinMax> Spawn = new();

        [DataField]
        public float Offset = 0.5f;

        [DataField("transferForensics")]
        public bool DoTransferForensics;

        [DataField]
        public bool SpawnInContainer;

        public void Execute(EntityUid owner,
            IDependencyCollection collection,
            EntityManager entManager,
            EntityUid? cause = null)
        {
            var protoManager = collection.Resolve<IPrototypeManager>();
            var random = collection.Resolve<IRobustRandom>();
            var stackSystem = entManager.System<SharedStackSystem>();
            var tSys = entManager.System<TransformSystem>();
            var destructSys = entManager.System<DestructibleSystem>();
            var position = tSys.GetMapCoordinates(owner);

            var getRandomVector = () => new Vector2(random.NextFloat(-Offset, Offset), random.NextFloat(-Offset, Offset));

            var executions = 1;
            if (entManager.TryGetComponent<StackComponent>(owner, out var stack))
            {
                executions = stack.Count;
            }

            foreach (var (entityId, minMax) in Spawn)
            {
                for (var execution = 0; execution < executions; execution++)
                {
                    var count = minMax.Min >= minMax.Max
                        ? minMax.Min
                        : random.Next(minMax.Min, minMax.Max + 1);

                    if (count == 0)
                        continue;

                    if (EntityPrototypeHelpers.HasComponent<StackComponent>(entityId, protoManager, entManager.ComponentFactory))
                    {
                        var spawned = SpawnInContainer
                            ? entManager.SpawnNextToOrDrop(entityId, owner)
                            : entManager.SpawnEntity(entityId, position.Offset(getRandomVector()));
                        stackSystem.SetCount(spawned, count);

                        TransferForensics(spawned, entManager, random, owner);
                    }
                    else
                    {
                        for (var i = 0; i < count; i++)
                        {
                            var spawned = SpawnInContainer
                                ? entManager.SpawnNextToOrDrop(entityId, owner)
                                : entManager.SpawnEntity(entityId, position.Offset(getRandomVector()));

                            TransferForensics(spawned, entManager, random, owner);
                        }
                    }
                }
            }
        }

        public void TransferForensics(EntityUid spawned, EntityManager entManager, IRobustRandom random, EntityUid owner)
        {
            if (!DoTransferForensics ||
                !entManager.TryGetComponent<ForensicsComponent>(owner, out var forensicsComponent))
            {
                return;
            }

            var comp = entManager.EnsureComponent<ForensicsComponent>(spawned);
            comp.DNAs = forensicsComponent.DNAs;

            if (!random.Prob(0.4f))

                return;
            comp.Fingerprints = forensicsComponent.Fingerprints;
            comp.Fibers = forensicsComponent.Fibers;
        }
    }
}
