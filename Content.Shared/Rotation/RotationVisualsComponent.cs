using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Rotation;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RotationVisualsComponent : Component
{
    /// <summary>
    /// Default value of <see cref="HorizontalRotation"/>
    /// </summary>
    [DataField]
    public Angle DefaultRotation = Angle.FromDegrees(90);

    [DataField]
    public Angle VerticalRotation = 0;

    [DataField, AutoNetworkedField]
    public Angle HorizontalRotation = Angle.FromDegrees(90);

    [DataField]
    public float AnimationTime = 0.125f;
}

[Serializable, NetSerializable]
public enum RotationVisuals
{
    RotationState
}

[Serializable, NetSerializable]
public enum RotationState
{
    /// <summary>
    ///     Standing up. This is the default value.
    /// </summary>
    Vertical = 0,

    /// <summary>
    ///     Laying down
    /// </summary>
    Horizontal,
}
