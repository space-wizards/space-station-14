using Content.Server.Stack;
using Content.Server.VendingMachineRestockPackage;
using Content.Shared.Prototypes;
using Content.Shared.VendingMachines;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    /// <summary>
    ///     Spawns a portion of the total items from one of the
    ///     canRestock inventory entries on a VendingMachineRestockPackage.
    /// </summary>
    [Serializable]
    [DataDefinition]
    public sealed class DumpRestockInventory: IThresholdBehavior
    {
        /// <summary>
        ///     The percent of each inventory entry that will be salvaged
        ///     upon destruction of the package.
        /// </summary>
        [DataField("percent", required: true)]
        public float Percent = 0.5f;

        [DataField("offset")]
        public float Offset { get; set; } = 0.5f;

        public void Execute(EntityUid owner, DestructibleSystem system)
        {
            if (!system.EntityManager.TryGetComponent<VendingMachineRestockPackageComponent>(owner, out var packagecomp) ||
                !system.EntityManager.TryGetComponent<TransformComponent>(owner, out var xform))
                return;

            var ary = new string[packagecomp.CanRestock.Count];

            packagecomp.CanRestock.CopyTo(ary);

            var randomInventory = ary[system.Random.Next(ary.Length)];

            if (!system.PrototypeManager.TryIndex(randomInventory, out VendingMachineInventoryPrototype? packPrototype))
                return;

            var position = system.EntityManager.GetComponent<TransformComponent>(owner).MapPosition;

            var getRandomVector = () => new Vector2(system.Random.NextFloat(-Offset, Offset), system.Random.NextFloat(-Offset, Offset));

            foreach (var (entityId, count) in packPrototype.StartingInventory)
            {
                var toSpawn = (int) Math.Round(count * Percent);

                if (toSpawn == 0) continue;

                if (EntityPrototypeHelpers.HasComponent<StackComponent>(entityId, system.PrototypeManager, system.ComponentFactory))
                {
                    var spawned = system.EntityManager.SpawnEntity(entityId, position.Offset(getRandomVector()));
                    system.StackSystem.SetCount(spawned, toSpawn);
                    system.EntityManager.GetComponent<TransformComponent>(spawned).LocalRotation = system.Random.NextAngle();
                }
                else
                {
                    for (var i = 0; i < toSpawn; i++)
                    {
                        var spawned = system.EntityManager.SpawnEntity(entityId, position.Offset(getRandomVector()));
                        system.EntityManager.GetComponent<TransformComponent>(spawned).LocalRotation = system.Random.NextAngle();
                    }
                }
            }
        }
    }
}
