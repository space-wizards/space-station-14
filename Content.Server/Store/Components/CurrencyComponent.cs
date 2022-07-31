using Content.Shared.Store;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Server.Store.Components;

[RegisterComponent]
public sealed class CurrencyComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("price", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<float, CurrencyPrototype>))]
    public Dictionary<string, float> Price = new();
}
