namespace Content.Server.Solar.Components;

[RegisterComponent]
public sealed partial class PowerSolarDataComponent : Component
{
    /// <summary>
    /// The current sun angle.
    /// </summary>
    [DataField]
    public Angle TowardsSun = Angle.Zero;

    /// <summary>
    /// The current sun angular velocity. (This is changed in Initialize)
    /// </summary>
    [DataField]
    public Angle SunAngularVelocity = Angle.Zero;

    /// <summary>
    /// TODO: *Should be moved into the solar tracker when powernet allows for it.*
    /// The current target panel rotation.
    /// </summary>
    [DataField]
    public Angle TargetPanelRotation = Angle.Zero;

    /// <summary>
    /// TODO: *Should be moved into the solar tracker when powernet allows for it.*
    /// The current target panel velocity.
    /// </summary>
    [DataField]
    public Angle TargetPanelVelocity = Angle.Zero;

    /// <summary>
    /// TODO: *Should be moved into the solar tracker when powernet allows for it.*
    /// Last update of total panel power.
    /// </summary>
    [DataField]
    public float TotalPanelPower = 0;
}
