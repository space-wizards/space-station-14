using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.Disposal.Holder;
using Content.Shared.Disposal.Tube;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.Disposal.Unit;

/// <summary>
/// Holder for entities being transported through the disposals system.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(new[] { typeof(DisposalTubeSystem), typeof(SharedDisposalHolderSystem) })]
public sealed partial class DisposalHolderComponent : Component, IGasMixtureHolder
{
    /// <summary>
    /// The container that holds entities being moved through the disposals system.
    /// </summary>
    [DataField]
    public Container? Container;

    /// <summary>
    /// Sets how fast the holder moves (~ number of tiles per second).
    /// </summary>
    [DataField]
    public float TraversalSpeed { get; set; } = 5f;

    /// <summary>
    /// Multiplier for how fast contained entities are ejected from open tubes.
    /// </summary>
    [DataField]
    public float ExitSpeedMultiplier { get; set; } = 1f;

    /// <summary>
    /// Multiplier for how far contained entities are ejected from open tubes.
    /// </summary>
    [DataField]
    public float ExitDistanceMultiplier { get; set; } = 1f;

    /// <summary>
    /// The disposal tube the holder is currently leaving.
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
    public bool IsExiting { get; set; } = false;

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
    /// Sound played when the holder changes direction.
    /// </summary>
    [DataField]
    public SoundSpecifier ClangSound = new SoundPathSpecifier("/Audio/Effects/clang.ogg", AudioParams.Default.WithVolume(-5f));

    /// <summary>
    /// Damage dealt to contained entities when the holder changes direction.
    /// </summary>
    [DataField]
    public DamageSpecifier DamageOnTurn = new()
    {
        DamageDict = new()
        {
            { "Blunt", 0.0 },
        }
    };

    /// <summary>
    /// The overall directional bias in the holder's movement. A directional bias greater than
    /// <see cref="DirectionBiasThreshold"/> indicates the entity may be stuck in a loop.
    /// </summary>
    [DataField]
    public float DirectionBias;

    /// <summary>
    /// If the absolute value of <see cref="DirectionBias"/> exceeds this threshold,
    /// the last disposal pipe the travelling entity entered is added to <see cref="SuspectPipes"/>.
    /// </summary>
    /// <remarks>
    /// For every multiple of four, the holder can make one theroretical loop of the disposal system
    /// before the threshold is exceeded. For example, if the threshold is 8, and the holder
    /// is inside a true loop, it will be able to escape once a third full loop is made.
    /// Do not set this value less than 4, it will result in more checks than are necessary.
    /// </remarks>
    [DataField]
    public float DirectionBiasThreshold = 8;

    /// <summary>
    /// The disposal pipes in this set are suspected as being part of a loop. If the holder
    /// enters any of these pipes again, it will potentially escape on subsequent turns.
    /// </summary>
    [DataField]
    public HashSet<EntityUid> SuspectPipes = new();

    /// <summary>
    /// Indicates whether the conditions have been met for the holder to potentially escape the disposals system.
    /// </summary>
    [DataField]
    public bool CanEscape;

    /// <summary>
    /// When given the opportunity, the holder has this chance of escaping the disposals system.
    /// </summary>
    [DataField]
    public float EscapeChance = 0.2f;

    /// <summary>
    /// Sets how many seconds mobs will be stunned after being ejected from an open tube.
    /// </summary>
    [DataField]
    public TimeSpan ExitStunDuration = TimeSpan.FromSeconds(1.5f);

    /// <summary>
    /// The amount of damage that has been accumulated by the holder contents.
    /// </summary>
    [DataField]
    public FixedPoint2 AccumulatedDamage = 0;

    /// <summary>
    /// Sets the maximum amount of damage that contained entities can suffer
    /// from changes in the holder's direction.
    /// </summary>
    [DataField]
    public FixedPoint2 MaxAllowedDamage = 50;
}

/// <summary>
/// Event raised when determining which direction a disposal holder should head next.
/// </summary>
/// <param name="Holder">The disposal holder.</param>
[ByRefEvent]
public record struct GetDisposalsNextDirectionEvent(Entity<DisposalHolderComponent> Holder)
{
    /// <summary>
    /// The next direction to move in.
    /// </summary>
    public Direction Next = Direction.Invalid;

    /// <summary>
    /// Sets whether the event has been handled by another system.
    /// </summary>
    public bool Handled;
}
