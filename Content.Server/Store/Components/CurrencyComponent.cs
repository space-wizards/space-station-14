using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Server.Store.Components;

/// <summary>
/// Identifies a component that can be inserted into a store
/// to increase its balance.
/// </summary>
/// <remarks>
/// Note that if this entity is a stack of items, then this is meant to represent the value per stack item, not
/// the whole stack. This also means that in general, the actual value should not be modified from the initial
/// prototype value because otherwise stack merging/splitting may modify the total value.
/// </remarks>
[RegisterComponent]
public sealed partial class CurrencyComponent : Component
{
    /// <summary>
    /// The value of the currency.
    /// The string is the currency type that will be added.
    /// The FixedPoint2 is the value of each individual currency entity.
    /// </summary>
    /// <remarks>
    /// Note that if this entity is a stack of items, then this is meant to represent the value per stack item, not
    /// the whole stack. This also means that in general, the actual value should not be modified from the initial
    /// prototype value
    /// because otherwise stack merging/splitting may modify the total value.
    /// </remarks>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("price", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<FixedPoint2, CurrencyPrototype>))]
    public Dictionary<string, FixedPoint2> Price = new();
}
