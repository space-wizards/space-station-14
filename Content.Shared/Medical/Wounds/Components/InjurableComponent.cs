using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounds.Prototypes;
using Content.Shared.Medical.Wounds.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.Medical.Wounds.Components;

[RegisterComponent, NetworkedComponent]
public sealed class InjurableComponent : Component
{
    [Access(typeof(InjurySystem), Other = AccessPermissions.Read)] [DataField("injuries")]
    public List<Injury>? Injuries;

    [Access(typeof(InjurySystem), Other = AccessPermissions.Read)]
    [DataField("allowedTraumaTypes", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<TraumaTypePrototype>))]
    public HashSet<string>? AllowedTraumaTypes;

    [Access(typeof(InjurySystem), Other = AccessPermissions.Read)] [ViewVariables, DataField("traumaResistance")]
    public TraumaModifierSet? TraumaResistance;

    [Access(typeof(InjurySystem), Other = AccessPermissions.Read)] [ViewVariables, DataField("traumaPenResistance")]
    public TraumaModifierSet? TraumaPenResistance;

    [Access(typeof(InjurySystem), Other = AccessPermissions.Read)] [ViewVariables, DataField("allowBleeds")]
    public bool AllowBleeds = true;

    [Access(typeof(InjurySystem), Other = AccessPermissions.Read)] [ViewVariables, DataField("appliesPain")]
    public bool AppliesPain = true;

    //How much health does this woundable have, when this reaches 0, it starts taking structural damage
    [Access(typeof(InjurySystem), Other = AccessPermissions.Read)]
    [ViewVariables, DataField("maxHealth", required: true)]
    public FixedPoint2 MaxHealth;

    //How much health does this woundable have, when this reaches 0, it starts taking structural damage
    [Access(typeof(InjurySystem), Other = AccessPermissions.Read)] [ViewVariables, DataField("health", required: true)]
    public FixedPoint2 Health;

    //How well is this woundable holding up, when this reaches 0 the entity is destroyed/gibbed!
    [Access(typeof(InjurySystem), Other = AccessPermissions.Read)]
    [ViewVariables, DataField("maxStructure", required: true)]
    public FixedPoint2 MaxStructure;

    //How well is this woundable holding up, when this reaches 0 the entity is destroyed/gibbed!
    [Access(typeof(InjurySystem), Other = AccessPermissions.Read)]
    [ViewVariables, DataField("structure", required: true)]
    public FixedPoint2 StructuralPool;
}

[DataRecord, NetSerializable, Serializable]
public record struct Injury(string InjuryId, FixedPoint2 Severity, FixedPoint2 Healed, FixedPoint2 Bleed,
    FixedPoint2 Infected)
{
    public Injury(string injuryId, FixedPoint2 severity) : this(injuryId, severity, FixedPoint2.Zero, FixedPoint2.Zero,
        FixedPoint2.Zero)
    {
    }
}
