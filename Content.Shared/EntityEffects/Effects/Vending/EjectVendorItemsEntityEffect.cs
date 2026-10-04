namespace Content.Shared.EntityEffects.Effects.Vending;

/// <summary>
/// See serverside system.
/// </summary>
/// <inheritdoc cref="EntityEffect"/>
public sealed partial class EjectVendorItems : EntityEffectBase<EjectVendorItems>
{
    /// <summary>
    /// The percent amount of the total inventory that will be ejected.
    /// </summary>
    [DataField(required: true)]
    public float Percent = 0.25f;

    /// <summary>
    /// The maximum amount of vendor items it can eject.
    /// Useful for high-inventory vendors.
    /// </summary>
    [DataField]
    public int Max = 3;
}
