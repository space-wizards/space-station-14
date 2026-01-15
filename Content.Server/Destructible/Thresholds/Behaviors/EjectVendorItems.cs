using Content.Server.VendingMachines;
using Content.Shared.Destructible.Thresholds.Behaviors;
using Content.Shared.VendingMachines;

namespace Content.Server.Destructible.Thresholds.Behaviors;

/// <summary>
///     Throws out a specific amount of random items from a vendor
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class EjectVendorItems : EntitySystem, IThresholdBehavior
{
    [Dependency] private readonly VendingMachineSystem _vendingMachine = default!;

    /// <summary>
    ///     The percent amount of the total inventory that will be ejected.
    /// </summary>
    [DataField(required: true)]
    public float Percent = 0.25f;

    /// <summary>
    ///     The maximum amount of vendor items it can eject
    ///     useful for high-inventory vendors
    /// </summary>
    [DataField]
    public int Max = 3;

    public void Execute(EntityUid owner, EntityUid? cause = null)
    {
        if (!TryComp<VendingMachineComponent>(owner, out var vendingcomp))
            return;

        var inventory = _vendingMachine.GetAvailableInventory(owner, vendingcomp);
        if (inventory.Count <= 0)
            return;

        var toEject = Math.Min(inventory.Count * Percent, Max);
        for (var i = 0; i < toEject; i++)
        {
            _vendingMachine.EjectRandom(owner, throwItem: true, forceEject: true, vendingcomp);
        }
    }
}

