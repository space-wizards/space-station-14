using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Store;

/// <summary>
///     Prototype used to define different types of currency for generic stores.
///     Mainly used for antags, such as traitors, nukies, and revenants
///     This is separate to the cargo ordering system.
/// </summary>
[Prototype("currency")]
[DataDefinition, Serializable, NetSerializable]
public sealed class CurrencyPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// The Loc string used for displaying the balance of a certain currency at the top of the store ui
    /// </summary>
    [DataField("balanceDisplay")]
    public string BalanceDisplay { get; } = string.Empty;

    /// <summary>
    /// The Loc string used for displaying the price of listings in store UI
    /// </summary>
    [DataField("priceDisplay")]
    public string PriceDisplay { get; } = string.Empty;

    /// <summary>
    /// The physical entity of the currency
    /// </summary>
    [DataField("entityId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? EntityId { get; }

    /// <summary>
    /// Whether or not this currency can be withdrawn from a shop by a player. Requires a valid entityId.
    /// </summary>
    [DataField("canWithdraw")]
    public bool CanWithdraw { get; } = true;
}
