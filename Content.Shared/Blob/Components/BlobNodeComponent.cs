using Robust.Shared.GameStates;

namespace Content.Shared.Blob.Components;

/// <summary>
/// This is used for a blob structure that pulses other nearby blob structures.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBlobSystem))]
public sealed partial class BlobNodeComponent : Component
{
    public const float BasePulseRange = 2.5f;

    /// <summary>
    /// The range in which nearby blob structures will be pulsed.
    /// </summary>
    [DataField]
    public float PulseRange = BasePulseRange;

    public float PulseRangeSquared => PulseRange * PulseRange;
}

/// <summary>
/// Event raised on a blob structure when it's pulsed state changes.
/// </summary>
/// <param name="Pulsed"></param>
[ByRefEvent]
public readonly record struct BlobPulsedSetEvent(bool Pulsed);
