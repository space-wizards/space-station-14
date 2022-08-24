using Content.Server.VendingMachines;
using Content.Shared.Throwing;
using Robust.Shared.Random;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    /// <summary>
    ///     Throws out a specific amount of random items from a vendor
    /// </summary>
    [Serializable]
    [DataDefinition]
    public sealed class EjectVendorItems : IThresholdBehavior
    {
        /// <summary>
        ///     The percent amount of the total inventory that will be ejected.
        /// </summary>
        [DataField("percent", required: true)]
        public float Percent = 0.25f;

        /// <summary>
        ///     The maximum amount of vendor items it can eject
        ///     useful for high-inventory vendors
        /// </summary>
        [DataField("max")]
        public int Max = 3;

        public void Execute(EntityUid owner, DestructibleSystem system)
        {
            if (!system.EntityManager.TryGetComponent<VendingMachineComponent>(owner, out var vendingcomp) ||
                !system.EntityManager.TryGetComponent<TransformComponent>(owner, out var xform))
                return;

            var throwingsys = system.EntityManager.EntitySysManager.GetEntitySystem<ThrowingSystem>();
            var totalItems = vendingcomp.AllInventory.Count;

            var toEject = Math.Min(totalItems * Percent, Max);
            for (var i = 0; i < toEject; i++)
            {
                var entity = system.EntityManager.SpawnEntity(system.Random.PickAndTake(vendingcomp.AllInventory).ID, xform.Coordinates);

                float range = vendingcomp.NonLimitedEjectRange;
                Vector2 direction = new Vector2(system.Random.NextFloat(-range, range), system.Random.NextFloat(-range, range));
                throwingsys.TryThrow(entity, direction, vendingcomp.NonLimitedEjectForce);
            }
        }
    }
}
