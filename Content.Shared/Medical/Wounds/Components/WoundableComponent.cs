using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounds.Prototypes;
using Content.Shared.Medical.Wounds.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.Medical.Wounds.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(WoundSystem))]
public sealed class WoundableComponent : Component
{
    [DataField("allowedTraumaTypes", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<TraumaPrototype>))]
    public HashSet<string>? AllowedTraumaTypes;

    [DataField("traumaResistance")] public TraumaModifierSet? TraumaResistance;

    [DataField("traumaPenResistance")] public TraumaModifierSet? TraumaPenResistance;

    //How much health does this woundable have, when this reaches 0, it starts taking structural damage
    [DataField("health")] public FixedPoint2 Health;

    //How well is this woundable holding up, when this reaches 0 the entity is destroyed/gibbed!
    [DataField("integrity", required: true)]
    public FixedPoint2 Integrity;

    [DataField("destroyWound", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? DestroyWoundId;
}
