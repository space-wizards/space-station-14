using Content.Shared.Atmos;
using Content.Shared.FixedPoint;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Conduit.Holder;

/// <summary>
/// Holder of <see cref="ConduitHeldComponent"/> entities being transported through a conduit.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(new[] { typeof(SharedConduitSystem), typeof(SharedConduitHolderSystem) })]
public sealed partial class ConduitHolderComponent : Component, IGasMixtureHolder
{
    /// <summary>
    /// The container that holds any entities being transported.
    /// </summary>
    [DataField]
    public Container? Container;

    /// <summary>
    /// Sets how fast the holder traverses conduits (~ number of tiles per second).
    /// </summary>
    [DataField]
    public float TraversalSpeed { get; set; } = 5f;

    /// <summary>
    /// Multiplier for how fast contained entities are ejected from a conduit.
    /// </summary>
    [DataField]
    public float ExitSpeedMultiplier { get; set; } = 1f;

    /// <summary>
    /// Multiplier for how far contained entities are ejected from a conduit.
    /// </summary>
    [DataField]
    public float ExitDistanceMultiplier { get; set; } = 1f;

    /// <summary>
    /// The conduit the holder is currently exiting.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? CurrentConduit { get; set; }

    /// <summary>
    /// The conduit the holder is moving towards.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? NextConduit { get; set; }

    /// <summary>
    /// The current direction the holder is moving.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Direction CurrentDirection { get; set; } = Direction.Invalid;

    /// <summary>
    /// Is set when the holder is leaving the conduit system.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsExiting { get; set; } = false;

    /// <summary>
    /// A list of tags attached to the holder. Used for routing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<string> Tags { get; set; } = new();

    /// <summary>
    /// The gas mixture contained in the holder.
    /// </summary>
    [DataField, AutoNetworkedField]
    public GasMixture Air { get; set; } = new(70);

    /// <summary>
    /// Tracks the number of times the entity changes direction
    /// </summary>
    [DataField]
    public int DirectionChangeCount;

    /// <summary>
    /// After <see cref="DirectionChangeCount"/> exceeds this value, this entity
    /// has a chance to escape the conduit system each time it changes direction
    /// (as set by <see cref="EscapeChance"/>).
    /// </summary>
    [DataField]
    public int DirectionChangeThreshold = 15;

    /// <summary>
    /// After this entity has lived for this lenght of time, it has a chance to escape the
    /// conduit system whenever it changes direction (as set by <see cref="EscapeChance"/>).
    /// </summary>
    [DataField]
    public TimeSpan LifeTime = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The time after which the entity has a chance to escape the conduit system
    /// whenever it changes direction (as set by <see cref="EscapeChance"/>).
    /// </summary>
    [DataField]
    public TimeSpan EndTime = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The base chance the entity has of escaping the conduit system.
    /// </summary>
    [DataField]
    public float EscapeChance = 0.1f;

    /// <summary>
    /// Sets how many seconds mobs will be stunned after being ejected from a conduit.
    /// </summary>
    [DataField]
    public TimeSpan ExitStunDuration = TimeSpan.FromSeconds(1.5f);

    /// <summary>
    /// The amount of damage that has been accumulated from changing directions.
    /// </summary>
    [DataField]
    public FixedPoint2 AccumulatedDamage = 0;

    /// <summary>
    /// Sets the maximum amount of damage that contained entities can suffer.
    /// from changing directions.
    /// </summary>
    [DataField]
    public FixedPoint2 MaxAllowedDamage = 50;

    /// <summary>
    /// Effect that gets played when the holder is to be deleted.
    /// </summary>
    [DataField]
    public EntProtoId? DespawnEffect;
}
