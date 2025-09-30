using Content.Shared.Actions;
using Robust.Shared.Audio;

namespace Content.Shared._Starlight.Antags.Actions;

public sealed partial class EMPScreamEvent : InstantActionEvent
{
    /// <summary>
    /// Determines power of scream, this affects the range and duration of the EMP.
    /// </summary>
    [DataField]
    public float Power = 2.5f;

    /// <summary>
    /// Determines, how many times should we increase the duration depending on the power. Example: Power * DurationMultiply; 2.5 * 2 = 5 seconds.
    /// </summary>
    [DataField]
    public float DurationMultiply = 2f;

    /// <summary>
    /// Determines, how much energy we should consume when use emp scream
    /// </summary>
    [DataField]
    public float EnergyConsumption = 5000f;

    [DataField]
    public SoundSpecifier? ScreamSound = new SoundPathSpecifier("/Audio/Effects/changeling_shriek.ogg");
}