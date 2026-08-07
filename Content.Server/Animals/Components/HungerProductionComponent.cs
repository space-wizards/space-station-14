using Content.Server.Animals.Systems;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Animals.Components;

/// <summary>
/// Periodically attempts to produce something, consuming hunger on success.
/// The actual product is supplied by a handler for <see cref="ProductionAttemptEvent"/>.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(HungerProductionSystem))]
public sealed partial class HungerProductionComponent : Component
{
    /// <summary>
    /// Entity whose life state and hunger are used for production.
    /// </summary>
    [DataField("owner")]
    public HungerProductionOwner OwnerEntity = HungerProductionOwner.Self;

    /// <summary>
    /// Minimum delay between automatic production attempts.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Optional maximum delay. When set, each automatic delay is randomized.
    /// </summary>
    [DataField]
    public TimeSpan? DelayMax;

    /// <summary>
    /// Hunger removed after successful production.
    /// </summary>
    [DataField]
    public float HungerUsage = 10f;

    /// <summary>
    /// Optional hunger threshold required before production.
    /// </summary>
    [DataField]
    public HungerThreshold? MinimumHungerThreshold;

    /// <summary>
    /// If set, entities with a HungerComponent must have at least this much hunger.
    /// </summary>
    [DataField]
    public float? MinimumHunger;

    /// <summary>
    /// Whether production is attempted automatically.
    /// </summary>
    [DataField]
    public bool Automatic = true;

    /// <summary>
    /// Whether player-controlled owners use automatic production.
    /// </summary>
    [DataField]
    public bool AutomaticForPlayers = true;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextProductionTime;
}

public enum HungerProductionOwner : byte
{
    Self,
    Parent
}

public enum HungerProductionFailure : byte
{
    None,
    Dead,
    Hungry,
    ProductUnavailable
}

/// <summary>
/// Raised on an entity producer after one or more entities have been produced.
/// </summary>
[ByRefEvent]
public record struct ProductionAttemptEvent(EntityUid Owner)
{
    public bool Produced;
}
