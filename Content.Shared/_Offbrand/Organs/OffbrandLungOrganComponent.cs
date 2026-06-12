using Content.Shared.Atmos;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Offbrand.Organs;

[RegisterComponent, NetworkedComponent]
public sealed partial class OffbrandLungOrganComponent : Component
{
    /// <summary>
    /// The damage type to use when computing oxygenation from the lungs
    /// </summary>
    [DataField(required: true)]
    public ProtoId<DamageTypePrototype> AsphyxiationDamage;

    /// <summary>
    /// The amount of <see cref="AsphyxiationDamage" /> at which lung oxygenation is considered to be 0%
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 AsphyxiationThreshold;

    /// <summary>
    /// Breath volume to depth descriptions.
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<float, OffbrandLungBreathingDepth> StethoscopeDepthDescriptions;

    /// <summary>
    /// Lung damage to regularity descriptions.
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<FixedPoint2, OffbrandLungBreathingRegularity> StethoscopeRegularityDescriptions;

    /// <summary>
    /// Respiratory rate to speed descriptions.
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<FixedPoint2, OffbrandLungBreathingSpeed> StethoscopeSpeedDescriptions;

    /// <summary>
    /// The stethoscope description.
    /// </summary>
    [DataField(required: true)]
    public LocId StethoscopeDescription;
}

[Serializable, NetSerializable]
public enum OffbrandLungBreathingDepth : byte
{
    Normal,
    Shallow,
}

[Serializable, NetSerializable]
public enum OffbrandLungBreathingRegularity : byte
{
    Regular,
    Irregular,
}

[Serializable, NetSerializable]
public enum OffbrandLungBreathingSpeed : byte
{
    Normal,
    Fast,
    Faster,
    Fastest,
}

/// <summary>
/// Event raised when an entity is about to take a breath
/// </summary>
/// <param name="BreathVolume">The volume to breathe in.</param>
[ByRefEvent]
public record struct BeforeBreathEvent(float BreathVolume);

/// <summary>
/// Event raised when an entity successfully inhales a gas, before storing the gas internally
/// </summary>
/// <param name="Gas">The gas we're inhaling.</param>
[ByRefEvent]
public record struct BeforeInhaledGasEvent(GasMixture Gas);
