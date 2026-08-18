using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Animation;

/// <summary>
/// When given to an entity, will create a sin wave offset of their sprite.
/// Can do both axies and also rotate.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SineWaveAnimationComponent : Component
{
    /// <summary>
    /// Length of the animation.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan AnimationLength = TimeSpan.FromSeconds(1);

    /// <summary>
    /// How many key frames should there in the animation?
    /// </summary>
    [DataField, AutoNetworkedField]
    public float KeyFrames = 60;

    /// <summary>
    /// If true, will reset the sprites offset back to its original value before the animation started when it finishes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ResetOffsetOnEnd = true;

    /// <summary>
    /// Saved offset from before the animation started.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public Vector2 StartOffset;

    /// <summary>
    /// If true, will reset the sprites rotation back to its original value before the animation started when it finishes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ResetRotationOnEnd = true;

    /// <summary>
    /// Saved rotation from before the animation started.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public Angle StartRotation;

    /// <summary>
    /// Should the animation repeat itself after being completed?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Repeat = true;

    /// <summary>
    /// Definition of the sin wave being played on the x-axis. If null, nothing will play on this axis.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SignWaveDefinition? XWave;

    /// <summary>
    /// Definition of the sin wave being played on the y-axis. If null, nothing will play on this axis.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SignWaveDefinition? YWave;

    /// <summary>
    /// Stores the total time for the x wave spent doing then animation. Needed for smooth looping so it doesn't
    /// offset in an odd way when restarting.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan TotalTimeX;

    /// <summary>
    /// Same as LastTimeX, but with Y.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan TotalTimeY;
}

/// <summary>
/// Definition of a sin wave. This is the equation that's used:
///
/// Period = 1 / Frequency
/// x = Amplitude * Sin((time + PhaseOffset) / Period)
/// </summary>
[Serializable, NetSerializable, DataDefinition]
public partial struct SignWaveDefinition
{
    /// <summary>
    /// Amplitude of the equation in distance (1 = 1 square offset at the peak)
    /// </summary>
    [DataField]
    public float Amplitude = 1;

    /// <summary>
    /// Frequency for the equation in Hz (1 = 1 cycle per second)
    /// </summary>
    [DataField]
    public float Frequency = 1;

    /// <summary>
    /// The time offset to start the animation. If set to null, it will be set to a random number
    /// between 0 and the Period (which is 1 / <see cref="Frequency"/>). Set to 0 for no offset.
    /// </summary>
    [DataField]
    public TimeSpan? PhaseOffset = TimeSpan.Zero;

    /// <summary>
    /// If true, the sprite will also be rotated to follow the slope of the wave.
    /// </summary>
    [DataField]
    public bool RotateToFollowSlope = true;
}
