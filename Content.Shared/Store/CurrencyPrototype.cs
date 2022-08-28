using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

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
    /// The Loc string used for displaying the currency in the store ui.
    /// doesn't necessarily refer to the full name of the currency, only
    /// that which is displayed to the user.
    /// </summary>
    [DataField("displayName")]
    public string DisplayName { get; } = string.Empty;

    /// <summary>
    /// The physical entity of the currency
    /// </summary>
    [DataField("cash")] //TODO: you get your customTypeSerializer when FixedPoint2 works in them! -emo
    public Dictionary<FixedPoint2, string>? Cash { get; }

    /// <summary>
    /// Whether or not this currency can be withdrawn from a shop by a player. Requires a valid entityId.
    /// </summary>
    [DataField("canWithdraw")]
    public bool CanWithdraw { get; } = true;
}
