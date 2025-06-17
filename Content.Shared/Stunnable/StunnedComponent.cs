using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared.Stunnable;

[RegisterComponent, AutoGenerateComponentState, NetworkedComponent, Access(typeof(SharedStunSystem))]
public sealed partial class StunnedComponent : Component
{
    /// <summary>
    /// Is this stun visualized?
    /// </summary>
    [AutoNetworkedField, DataField]
    public bool Visualized;

    /// <summary>
    /// When should our animation end?
    /// </summary>
    [AutoNetworkedField, DataField]
    public TimeSpan AnimationEnd;

    /// <summary>
    /// Max vector displacement for our jittering
    /// </summary>
    [DataField]
    public float Amplitude = 0.05f;

    /// <summary>
    /// X vector displacement for our heavy breathing
    /// </summary>
    [DataField]
    public float BreathingAmplitude = 0.05f;

    /// <summary>
    /// Max angular displacement for our jittering in radians
    /// </summary>
    [DataField]
    public float Torque = 0.08f;

    /// <summary>
    /// Total Animations per second
    /// </summary>
    [DataField]
    public float Frequency = 2f;

    /// <summary>
    /// Total Rotation Animations per second
    /// </summary>
    [DataField]
    public float RotationFrequency = 4f;

    /// <summary>
    ///     The last offset keyframe this animation had, so we can make sure the jitter direction
    ///     is never truly random
    /// </summary>
    [DataField]
    public Vector2 LastJitter;

    /// <summary>
    /// Jitters per Animation
    /// </summary>
    [DataField]
    public int Jitters = 4;

    /// <summary>
    ///     The offset that an entity had before animation has started,
    ///     so that we can reset it properly.
    /// </summary>
    [DataField]
    public Vector2 StartOffset = Vector2.Zero;

    [DataField]
    public Angle StartAngle = Angle.FromDegrees(90);
}
