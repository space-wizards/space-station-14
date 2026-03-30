using System.Numerics;
using Content.Server.Forensics;
using Content.Server.Spawners.Components;
using Content.Server.Spawners.EntitySystems;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.Prototypes;
using Content.Shared.Stacks;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
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
        public Dictionary<EntProtoId, MinMax> Spawn = new();

        [DataField("offset")]
        public float Offset { get; set; } = 0.5f;

        [DataField("transferForensics")]
        public bool DoTransferForensics;

        [DataField]
        public bool SpawnInContainer;

        /// <summary>
        /// Time in seconds to wait before spawning entities.
        /// </summary>
        [DataField]
        public float SpawnAfter;

        public void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null)
        {
            var tSys = system.EntityManager.System<TransformSystem>();
            var position = tSys.GetMapCoordinates(owner);

            var executions = 1;
            if (system.EntityManager.TryGetComponent<StackComponent>(owner, out var stack))
            {
                executions = stack.Count;
            }

            if (SpawnAfter != 0)
                ExecuteDelayedSpawn(system, position, executions);
            else
                ExecuteImmediateSpawn(system, owner, position, executions);
        }

        private void ExecuteDelayedSpawn(DestructibleSystem system, MapCoordinates position, int executions)
        {
            if (!system.PrototypeManager.Resolve(TempEntityProtoId, out var tempSpawnerProto))
                return;

            foreach (var (entityId, minMax) in Spawn)
            {
                for (var execution = 0; execution < executions; execution++)
                {
                    var count = minMax.Min >= minMax.Max
                        ? minMax.Min
                        : system.Random.Next(minMax.Min, minMax.Max + 1);

                    if (count == 0)
                        continue;

                    for (var i = 0; i < count; i++)
                    {
                        var offset = GetRandomOffset(system);
                        var spawner = system.EntityManager.SpawnEntity(tempSpawnerProto.ID, position.Offset(offset));
                        system.EntityManager.EnsureComponent<TimedDespawnComponent>(spawner, out var timedDespawnComponent);
                        timedDespawnComponent.Lifetime = SpawnAfter;
                        system.EntityManager.EnsureComponent<SpawnOnDespawnComponent>(spawner, out var spawnOnDespawnComponent);
                        system.EntityManager.System<SpawnOnDespawnSystem>().SetPrototype((spawner, spawnOnDespawnComponent), entityId);
                    }
                }
            }
        }

        private void ExecuteImmediateSpawn(DestructibleSystem system, EntityUid owner, MapCoordinates position, int executions)
        {
            foreach (var (entityId, minMax) in Spawn)
            {
                for (var execution = 0; execution < executions; execution++)
                {
                    var count = minMax.Min >= minMax.Max
                        ? minMax.Min
                        : system.Random.Next(minMax.Min, minMax.Max + 1);

                    if (count == 0)
                        continue;

                    if (EntityPrototypeHelpers.HasComponent<StackComponent>(entityId, system.PrototypeManager, system.EntityManager.ComponentFactory))
                    {
                        var spawned = SpawnInContainer
                            ? system.EntityManager.SpawnNextToOrDrop(entityId, owner)
                            : system.EntityManager.SpawnEntity(entityId, position.Offset(GetRandomOffset(system)));
                        system.StackSystem.SetCount((spawned, null), count);

                        TransferForensics(spawned, system, owner);
                    }
                    else
                    {
                        for (var i = 0; i < count; i++)
                        {
                            var spawned = SpawnInContainer
                                ? system.EntityManager.SpawnNextToOrDrop(entityId, owner)
                                : system.EntityManager.SpawnEntity(entityId, position.Offset(GetRandomOffset(system)));

                            TransferForensics(spawned, system, owner);
                        }
                    }
                }
            }
        }

        private Vector2 GetRandomOffset(DestructibleSystem system)
        {
            return new(system.Random.NextFloat(-Offset, Offset), system.Random.NextFloat(-Offset, Offset));
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
