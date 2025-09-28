using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Offbrand.Wounds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(PainSystem))]
public sealed partial class PainComponent : Component
{
    [DataField, AutoNetworkedField]
    public float UpdateIntervalMultiplier = 1f;

    [ViewVariables]
    public TimeSpan AdjustedUpdateInterval => UpdateInterval * UpdateIntervalMultiplier;

    /// <summary>
    /// How often the shock is recalculated
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The current amount of shock, which follows to <see cref="PainMultiplier"> times the amount of pain from the entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    [Access(typeof(PainSystem), Other = AccessPermissions.None)]
    public FixedPoint2 Shock = FixedPoint2.Zero;

    /// <summary>
    /// If this entity can feel pain.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Suppressed;

    /// <summary>
    /// Multiplier for far above the level of pain the shock will go to
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 PainMultiplier;

    /// <summary>
    /// How fast the shock can increase per second
    /// </summary>
    [DataField(required: true)]
    public double MaxShockIncreasePerSecond;

    /// <summary>
    /// How fast the shock can decrease per second
    /// </summary>
    [DataField(required: true)]
    public double MaxShockDecreasePerSecond;

    /// <summary>
    /// If the current pain is less than this number times the amount of shock, shock will decrease at double the rate
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 DoubleShockRecoveryThreshold;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? LastUpdate;
}

/// <summary>
/// Raised on an entity after its shock has changed
/// </summary>
[ByRefEvent]
public record struct AfterShockChangeEvent;

/// <summary>
/// Raised on an entity to determine if its pain should be suppressed
/// </summary>
[ByRefEvent]
public record struct PainSuppressionEvent(bool Suppressed);
