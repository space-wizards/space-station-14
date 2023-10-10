using Content.Server.VendingMachines;
using Content.Shared.VendingMachines;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    /// <summary>
    ///     Throws out a specific amount of random items from a vendor
    /// </summary>
    [Serializable]
    [DataDefinition]
    public sealed partial class EjectVendorItems : IThresholdBehavior
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

        public void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null)
        {
            if (!system.EntityManager.TryGetComponent<VendingMachineComponent>(owner, out var vendingcomp) ||
                !system.EntityManager.TryGetComponent<TransformComponent>(owner, out var xform))
                return;

            var vendingMachineSystem = EntitySystem.Get<VendingMachineSystem>();
            var inventory = vendingMachineSystem.GetAvailableInventory(owner, vendingcomp);
            if (inventory.Count <= 0)
                return;

            var toEject = Math.Min(inventory.Count * Percent, Max);
            for (var i = 0; i < toEject; i++)
            {
                vendingMachineSystem.EjectRandom(owner, throwItem: true, forceEject: true, vendingcomp);
            }
        }
    }
}
