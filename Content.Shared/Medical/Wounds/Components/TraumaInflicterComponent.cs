using Content.Shared.Medical.Wounds.Prototypes;
using Content.Shared.Medical.Wounds.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Medical.Wounds.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(WoundSystem))]
public sealed class TraumaInflicterComponent : Component
{
    [DataField("traumas", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<TraumaDamage, TraumaPrototype>))]
    public readonly Dictionary<string, TraumaDamage> Traumas = new();
}
