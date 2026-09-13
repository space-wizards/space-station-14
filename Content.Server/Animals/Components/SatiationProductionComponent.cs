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
    /// Selects the entity whose mob state and satiation are used for production checks and consumption.
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
    /// Amount of the configured satiation removed after successful production.
    /// </summary>
    [DataField]
    public float SatiationUsage = 10f;

    /// <summary>
    /// Satiation type checked and consumed by production. Defaults to hunger.
    /// </summary>
    [DataField]
    public ProtoId<SatiationTypePrototype> SatiationType = SatiationSystem.Hunger;

    /// <summary>
    /// Optional threshold that the configured satiation must still exceed after applying the production cost.
    /// </summary>
    [DataField]
    public SatiationValue? MinimumSatiationThreshold;

    /// <summary>
    /// Optional minimum numeric value of the configured satiation required before production.
    /// </summary>
    [DataField]
    public float? MinimumSatiation;

    /// <summary>
    /// Whether production is attempted automatically.
    /// </summary>
    [DataField]
    public bool Automatic = true;

    /// <summary>
    /// Whether automatic production is allowed for player-controlled producer entities.
    /// </summary>
    [DataField]
    public bool AutomaticForPlayers = true;

    /// <summary>
    /// Next scheduled automatic production attempt. Adjusted while the component is paused.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextProductionTime;
}

/// <summary>
/// Selects the entity against which production conditions are evaluated.
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

    /// <summary>
    /// The selected producer does not meet the configured satiation requirement.
    /// </summary>
    InsufficientSatiation,

    /// <summary>
    /// The production attempt completed without producing a product.
    /// </summary>
    ProductUnavailable
}

/// <summary>
/// Raised when production is attempted.
/// Handlers set <see cref="Produced"/> when something was successfully produced.
/// </summary>
/// <param name="Owner">Entity selected as the producer for this attempt.</param>
[ByRefEvent]
public record struct ProductionAttemptEvent(EntityUid Owner)
{
    /// <summary>
    /// Set by handlers when production succeeds.
    /// </summary>
    public bool Produced;
}
