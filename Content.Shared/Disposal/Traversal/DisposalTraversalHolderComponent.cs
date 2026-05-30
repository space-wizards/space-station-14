using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Disposal.Traversal;

/// <summary>
/// Holder for entities moving through a controllable disposal traversal network.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
public sealed partial class DisposalTraversalHolderComponent : Component
{
    /// <summary>
    /// Speed of traversal through segments, in tiles per second.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float TraversalSpeed = 6f;

    /// <summary>
    /// Whether movement input is currently held.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool IsMoving;

    /// <summary>
    /// Segment the holder is currently in.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? CurrentTube { get; set; }

    /// <summary>
    /// Segment the holder is moving towards, if any.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? NextTube { get; set; }

    /// <summary>
    /// Current movement direction requested by input.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public Direction CurrentDirection { get; set; } = Direction.Invalid;

    /// <summary>
    /// Current traversal layer. Interpretation belongs to the network-specific adapter.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public int CurrentLayer { get; set; }

    /// <summary>
    /// Minimum delay between traversal sounds.
    /// </summary>
    [DataField]
    public TimeSpan TraversalSoundDelay { get; set; } = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// Timestamp of the last traversal sound.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan LastTraversalSound;

    /// <summary>
    /// Sound played while moving through the network.
    /// </summary>
    [DataField]
    public SoundCollectionSpecifier TraversalSound { get; set; } = new("VentClaw", AudioParams.Default.WithVolume(5f));
}
