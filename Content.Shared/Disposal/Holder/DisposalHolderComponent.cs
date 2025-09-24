using Content.Shared.Atmos;
using Content.Shared.Disposal.Holder;
using Content.Shared.Disposal.Tube;
using Content.Shared.FixedPoint;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.Disposal.Unit;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(new[] { typeof(SharedDisposalTubeSystem), typeof(SharedDisposalHolderSystem) })]
public sealed partial class DisposalHolderComponent : Component, IGasMixtureHolder
{
    [DataField]
    public Container? Container;

    /// <summary>
    /// Sets how fast the holder traverses pipes.
    /// </summary>
    [DataField]
    public float TraversalSpeed { get; set; } = 5f;

    /// <summary>
    /// Multiplier for how fast contained entities are ejected from pipes.
    /// </summary>
    [DataField]
    public float ExitSpeedMultiplier { get; set; } = 1f;

    /// <summary>
    /// Multiplier for how far contained entities are ejected from pipes.
    /// </summary>
    [DataField]
    public float ExitDistanceMultiplier { get; set; } = 1f;

    /// <summary>
    /// The disposal tube the holder is currently exiting.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? CurrentTube { get; set; }

    /// <summary>
    /// The disposal tube the holder is moving towards.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? NextTube { get; set; }

    /// <summary>
    /// The current direction the holder is moving.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Direction CurrentDirection { get; set; } = Direction.Invalid;

    /// <summary>
    /// Is set when the holder is exiting disposals.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsExitingDisposals { get; set; } = false;

    /// <summary>
    /// A list of tags attached to the holder, used for routing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<string> Tags { get; set; } = new();

    /// <summary>
    /// The gas mixture contained in the holder.
    /// </summary>
    [DataField, AutoNetworkedField]
    public GasMixture Air { get; set; } = new(70);

    /// <summary>
    /// A dictionary containing the number of times the holder has passed through
    /// specific tubes. If the number of visits exceeds <see cref="TubeVisitThreshold"/>,
    /// the holder has a chance to break free of the disposal system, as set by
    /// <see cref="TubeEscapeChance"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<EntityUid, int> TubeVisits = new();

    /// <summary>
    /// The number of times the holder can pass through a tube before it has
    /// a chance to escape the disposals system.
    /// </summary>
    [DataField]
    public int TubeVisitThreshold = 1;

    /// <summary>
    /// The chance of the holder escaping the disposals system once the
    /// number of times it passes through the same pipe exceeds
    /// <see cref="TubeVisitThreshold"/>.
    /// </summary>
    [DataField]
    public float TubeEscapeChance = 0.2f;

    /// <summary>
    /// Sets how many seconds mobs will be stunned after being ejected from a pipe.
    /// </summary>
    [DataField]
    public TimeSpan DisposalExitStunDuration = TimeSpan.FromSeconds(1.5f);

    /// <summary>
    /// The amount of damage that has been accumulated from being bounced around
    /// the disposals system.
    /// </summary>
    [DataField]
    public FixedPoint2 AccumulatedDamage = 0;

    /// <summary>
    /// Sets the maximum amount of damage that contained entities can suffer
    /// from being bounced around the disposals system.
    /// </summary>
    [DataField]
    public FixedPoint2 MaxAllowedDamage = 50;
}
