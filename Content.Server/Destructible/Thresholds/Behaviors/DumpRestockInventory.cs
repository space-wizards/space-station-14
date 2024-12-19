using Content.Server.Stack;
using Content.Shared.Destructible.Thresholds.Behaviors;
using Robust.Shared.Random;
using Content.Shared.Stacks;
using Content.Shared.Prototypes;
using Content.Shared.VendingMachines;
using Robust.Shared.Prototypes;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    /// <summary>
    ///     Spawns a portion of the total items from one of the canRestock
    ///     inventory entries on a VendingMachineRestock component.
    /// </summary>
    [Serializable]
    [DataDefinition]
    public sealed partial class DumpRestockInventory: IThresholdBehavior
    {
        /// <summary>
        ///     The percent of each inventory entry that will be salvaged
        ///     upon destruction of the package.
        /// </summary>
        [DataField(required: true)]
        public float Percent = 0.5f;

        [DataField]
        public float Offset = 0.5f;

        public void Execute(EntityUid owner,
            IDependencyCollection collection,
            EntityManager entManager,
            EntityUid? cause = null)
        {
            if (!entManager.TryGetComponent<VendingMachineRestockComponent>(owner, out var packagecomp) ||
                !entManager.TryGetComponent<TransformComponent>(owner, out var xform))
            {
                return;
            }

            var protoManager = collection.Resolve<IPrototypeManager>();
            var random = collection.Resolve<IRobustRandom>();
            var stackSystem = entManager.System<StackSystem>();
            var randomInventory = random.Pick(packagecomp.CanRestock);

            if (!protoManager.TryIndex(randomInventory, out VendingMachineInventoryPrototype? packPrototype))
                return;

            foreach (var (entityId, count) in packPrototype.StartingInventory)
            {
                var toSpawn = (int) Math.Round(count * Percent);

                if (toSpawn == 0) continue;

                if (EntityPrototypeHelpers.HasComponent<StackComponent>(entityId, protoManager, entManager.ComponentFactory))
                {
                    var spawned = entManager.SpawnEntity(entityId, xform.Coordinates.Offset(random.NextVector2(-Offset, Offset)));
                    stackSystem.SetCount(spawned, toSpawn);
                    entManager.GetComponent<TransformComponent>(spawned).LocalRotation = random.NextAngle();
                }
                else
                {
                    for (var i = 0; i < toSpawn; i++)
                    {
                        var spawned = entManager.SpawnEntity(entityId, xform.Coordinates.Offset(random.NextVector2(-Offset, Offset)));
                        entManager.GetComponent<TransformComponent>(spawned).LocalRotation = random.NextAngle();
                    }
                }
            }
        }
    }
}
