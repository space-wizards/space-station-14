using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Rotation;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RotationVisualsComponent : Component
{
    /// <summary>
    /// Default value of <see cref="HorizontalRotation"/>
    /// </summary>
    ///
    //should we not be changing the default rotation to be either horizontal or vertical?
    [DataField]
    public Angle DefaultRotation = Angle.Zero; //should this not be 0 degrees? as the character stands up by default (as opposed to 90 degrees - laying down)

    [DataField]
    public Angle VerticalRotation = Angle.Zero;

    [DataField, AutoNetworkedField]
    public Angle HorizontalRotation = Angle.FromDegrees(180); //this should be -90, because the beds are all at rotation -90, regular 90 degrees will have you facing the wrong way

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
    Horizontal
}
