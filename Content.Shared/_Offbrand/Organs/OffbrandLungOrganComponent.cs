using Content.Shared.Atmos;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

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
