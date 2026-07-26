using System.Numerics;
using Content.Server.Forensics;
using Content.Server.Spawners.Components;
using Content.Server.Spawners.EntitySystems;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.Prototypes;
using Content.Shared.Stacks;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Spawners;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    [Serializable]
    [DataDefinition]
    public sealed partial class SpawnEntitiesBehavior : IThresholdBehavior
    {
        private static readonly EntProtoId TempEntityProtoId = "TemporaryEntityForTimedDespawnSpawners";

        /// <summary>
        ///     Entities spawned on reaching this threshold, from a min to a max.
        /// </summary>
        [DataField]
        public Dictionary<EntProtoId, MinMax> Spawn = [];

        [DataField]
        public float Offset { get; set; } = 0.5f;

        [DataField("transferForensics")]
        public bool DoTransferForensics;

        [DataField]
        public bool SpawnInContainer;

        /// <summary>
        /// Time in seconds to wait before spawning entities.
        /// </summary>
        /// <remarks>
        /// If positive, <see cref="DoTransferForensics"/> and <see cref="SpawnInContainer"/> are ignored.
        /// </remarks>
        [DataField]
        public float SpawnAfter;

        public void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null)
        {
            var tSys = system.EntityManager.System<TransformSystem>();
            var position = tSys.GetMapCoordinates(owner);

            Vector2 GetRandomVector() => system.Random.NextVector2(Offset);

            var executions = 1;
            if (system.EntityManager.TryGetComponent<StackComponent>(owner, out var stack))
            {
                executions = stack.Count;
            }

            foreach (var (entityId, minMax) in Spawn)
            {
                for (var execution = 0; execution < executions; execution++)
                {
                    var count = minMax.Min >= minMax.Max
                        ? minMax.Min
                        : system.Random.Next(minMax.Min, minMax.Max + 1);

                    if (count == 0)
                        continue;

                    if (SpawnAfter > 0)
                    {
                        // TODO: TransferForensics is not supported here because the actual entity UID is not known until the spawner spawns it.
                        for (var i = 0; i < count; i++)
                        {
                            var spawner = system.EntityManager.SpawnEntity(TempEntityProtoId, position.Offset(GetRandomVector()));
                            var timedDespawn = system.EntityManager.GetComponent<TimedDespawnComponent>(spawner);
                            timedDespawn.Lifetime = SpawnAfter;
                            var spawnOnDespawn = system.EntityManager.GetComponent<SpawnOnDespawnComponent>(spawner);
                            system.EntityManager.System<SpawnOnDespawnSystem>().SetPrototype((spawner, spawnOnDespawn), entityId);
                        }
                    }
                    else if (EntityPrototypeHelpers.HasComponent<StackComponent>(entityId, system.PrototypeManager, system.EntityManager.ComponentFactory))
                    {
                        var spawned = SpawnInContainer
                            ? system.EntityManager.SpawnNextToOrDrop(entityId, owner)
                            : system.EntityManager.SpawnEntity(entityId, position.Offset(GetRandomVector()));
                        system.StackSystem.SetCount((spawned, null), count);

                        TransferForensics(spawned, system, owner);
                    }
                    else
                    {
                        for (var i = 0; i < count; i++)
                        {
                            var spawned = SpawnInContainer
                                ? system.EntityManager.SpawnNextToOrDrop(entityId, owner)
                                : system.EntityManager.SpawnEntity(entityId, position.Offset(GetRandomVector()));

                            TransferForensics(spawned, system, owner);
                        }
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
