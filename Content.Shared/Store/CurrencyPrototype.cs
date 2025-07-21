using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Store;

/// <summary>
///     Prototype used to define different types of currency for generic stores.
///     Mainly used for antags, such as traitors, nukies, and revenants
///     This is separate to the cargo ordering system.
/// </summary>
[Prototype]
[DataDefinition, Serializable, NetSerializable]
public sealed partial class CurrencyPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The Loc string used for displaying the currency in the store ui.
    /// doesn't necessarily refer to the full name of the currency, only
    /// that which is displayed to the user.
    /// </summary>
    [DataField("displayName")]
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// The physical entity of the currency
    /// </summary>
    [DataField("cash", customTypeSerializer: typeof(PrototypeIdValueDictionarySerializer<FixedPoint2, EntityPrototype>))]
    public Dictionary<FixedPoint2, string>? Cash { get; private set; }

    /// <summary>
    /// Whether or not this currency can be withdrawn from a shop by a player. Requires a valid entityId.
    /// </summary>
    [DataField("canWithdraw")]
    public bool CanWithdraw { get; private set; } = true;
}
