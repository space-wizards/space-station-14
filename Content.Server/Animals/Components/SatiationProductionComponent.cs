using Content.Server.Animals.Systems;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Animals.Components;

/// <summary>
/// Periodically attempts to produce something, consuming satiation on success.
/// The actual product is supplied by a handler for <see cref="ProductionAttemptEvent"/>.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(SatiationProductionSystem))]
public sealed partial class SatiationProductionComponent : Component
{
    /// <summary>
    /// Entity whose life state and satiation are used for production.
    /// </summary>
    [DataField]
    public SatiationProductionOwner Producer = SatiationProductionOwner.Self;

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
    /// Satiation removed after successful production.
    /// </summary>
    [DataField]
    public float SatiationUsage = 10f;

    /// <summary>
    /// Satiation type used for production.
    /// </summary>
    [DataField]
    public ProtoId<SatiationTypePrototype> SatiationType = SatiationSystem.Hunger;

    /// <summary>
    /// Optional satiation threshold which must remain exceeded after production.
    /// </summary>
    [DataField]
    public SatiationValue? MinimumSatiationThreshold;

    /// <summary>
    /// If set, entities with the configured satiation must have at least this value.
    /// </summary>
    [DataField]
    public float? MinimumSatiation;

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

/// <summary>
/// Entity used for production checks and satiation consumption.
/// </summary>
public enum SatiationProductionOwner : byte
{
    Self,
    Parent
}

/// <summary>
/// Reason a production attempt failed.
/// </summary>
public enum SatiationProductionFailure : byte
{
    None,
    Dead,
    InsufficientSatiation,
    ProductUnavailable
}

/// <summary>
/// Raised when production is attempted.
/// Handlers set <see cref="Produced"/> when something was successfully produced.
/// </summary>
[ByRefEvent]
public record struct ProductionAttemptEvent(EntityUid Owner)
{
    /// <summary>
    /// Set by handlers when production succeeds.
    /// </summary>
    public bool Produced;
}
