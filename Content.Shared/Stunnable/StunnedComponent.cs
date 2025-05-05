using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared.Stunnable;

[RegisterComponent, AutoGenerateComponentState, NetworkedComponent, Access(typeof(SharedStunSystem))]
public sealed partial class StunnedComponent : Component
{
    /// <summary>
    /// When should our animation end?
    /// </summary>
    [AutoNetworkedField, DataField]
    public TimeSpan AnimationEnd;

    /// <summary>
    /// Max vector displacement for our jittering
    /// </summary>
    [AutoNetworkedField, DataField]
    public float Amplitude = 0.05f;

    /// <summary>
    /// X vector displacement for our heavy breathing
    /// </summary>
    [AutoNetworkedField, DataField]
    public float BreathingAmplitude = 0.05f;

    /// <summary>
    /// Max angular displacement for our jittering in radians
    /// </summary>
    [AutoNetworkedField, DataField]
    public float Torque = 0.08f;

    /// <summary>
    /// Total Animations per second
    /// </summary>
    [AutoNetworkedField, DataField]
    public float Frequency = 3f;

    /// <summary>
    /// Total Rotation Animations per second
    /// </summary>
    [AutoNetworkedField, DataField]
    public float RotationFrequency = 4f;

    /// <summary>
    ///     The last offset keyframe this animation had, so we can make sure the jitter direction
    ///     is never truly random
    /// </summary>
    [AutoNetworkedField, DataField]
    public Vector2 LastJitter;

    /// <summary>
    /// Jitters per Animation
    /// </summary>
    [AutoNetworkedField, DataField]
    public int Jitters = 4;

    /// <summary>
    ///     The offset that an entity had before animation has started,
    ///     so that we can reset it properly.
    /// </summary>
    [AutoNetworkedField, DataField]
    public Vector2 StartOffset = Vector2.Zero;

    [AutoNetworkedField, DataField]
    public Angle StartAngle = Angle.FromDegrees(90);
}
