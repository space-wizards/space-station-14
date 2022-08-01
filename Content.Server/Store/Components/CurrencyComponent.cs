using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Server.Store.Components;

[RegisterComponent]
public sealed class CurrencyComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("price", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<FixedPoint2, CurrencyPrototype>))]
    public Dictionary<string, FixedPoint2> Price = new();
}
