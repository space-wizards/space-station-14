using Content.Shared.Body;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._Offbrand.Organs;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(OffbrandHeartOrganSystem))]
public sealed partial class OffbrandHeartOrganComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Compensation = 1f;

    [DataField(required: true)]
    public float CompensationCoefficient;

    [DataField(required: true)]
    public float CompensationConstant;

    [DataField(required: true)]
    public float CompensationStrainCoefficient;

    [DataField(required: true)]
    public float CompensationStrainConstant;

    /// <summary>
    /// How much damage to inflict on the heart depending on strain.
    /// - Chance: the chance to inflict damage
    /// - Amount: how much damage to inflict
    /// The highest amount is chosen.
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<FixedPoint2, (double Chance, FixedPoint2 Amount)> StrainDamageThresholds;

    [DataField, AutoNetworkedField]
    public bool Beating = true;
}

[RegisterComponent]
[Access(typeof(OffbrandHeartOrganSystem))]
public sealed partial class HeartStopOnHighStrainComponent : Component
{
    /// <summary>
    /// How likely the heart is to stop when the strain threshold is exceeded
    /// </summary>
    [DataField(required: true)]
    public float Chance;

    /// <summary>
    /// The minimum threshold at which the heart can stop from strain
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 Threshold;

    /// <summary>
    /// The warning issued by defibrillators if the heart is restarted with high strain
    /// </summary>
    [DataField]
    public LocId Warning = "heart-defibrillatable-target-strain";
}

[RegisterComponent]
[Access(typeof(OffbrandHeartOrganSystem))]
public sealed partial class HeartDefibrillatableComponent : Component
{
    [DataField]
    public LocId TargetIsDead = "heart-defibrillatable-target-is-dead";
}

/// <summary>
/// Raised on an organ during a heartbeat
/// </summary>
[ByRefEvent]
public record struct PotentialHeartStopEvent(Entity<BodyComponent> Body, bool Stop);

/// <summary>
/// Raised on an entity if the heart has stopped beating
/// </summary>
[ByRefEvent]
public record struct HeartStoppedEvent;

/// <summary>
/// Raised on an entity if the heart has started beating
/// </summary>
[ByRefEvent]
public record struct HeartStartedEvent;

/// <summary>
/// Raised on an entity to see if the defibrillator will say anything before defibrillation
/// </summary>
[ByRefEvent]
public record struct BeforeTargetDefibrillatedEvent(List<LocId> Messages);
