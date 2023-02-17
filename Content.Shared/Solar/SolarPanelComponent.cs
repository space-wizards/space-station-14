using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Solar.Components;

/// <summary>
///     This is a solar panel.
///     It generates power from the sun based on coverage.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
[Access(typeof(SharedPowerSolarSystem))]
public sealed class SolarPanelComponent : Component
{
    /// <summary>
    /// Maximum supply output by this panel (coverage = 1)
    /// </summary>
    [DataField("maxSupply")]
    [ViewVariables]
    public int MaxSupply = 1500;

    /// <summary>
    /// Current coverage of this panel (from 0 to 1).
    /// This is updated by <see cref='PowerSolarSystem'/>.
    /// DO NOT WRITE WITHOUT CALLING UpdateSupply()!
    /// </summary>
    [ViewVariables]
    public float Coverage = 0;

    /// <summary>
    /// Current solar panel angle (updated by <see cref="PowerSolarSystem"/>)
    /// </summary>
    [DataField("angle")]
    [ViewVariables]
    public Angle Angle = Angle.Zero;

    /// <summary>
    /// Current solar panel angular velocity
    /// </summary>
    [DataField("angularVelocity")]
    [ViewVariables(VVAccess.ReadWrite)]
    public Angle AngularVelocity = Angle.Zero;

    /// <summary>
    /// Solar panel angle at <see cref="LastUpdate"/> time
    /// </summary>
    [DataField("startAngle")]
    [ViewVariables]
    public Angle StartAngle = Angle.Zero;

    /// <summary>
    /// Timestamp of the last update. (used for <see cref="Angle"/> calculation)
    /// </summary>
    [DataField("lastUpdate", customTypeSerializer: typeof(TimeOffsetSerializer))]
    [ViewVariables]
    public TimeSpan LastUpdate = TimeSpan.Zero;
}

[Serializable, NetSerializable]
public sealed class SolarPanelComponentState : ComponentState
{
    public Angle Angle;
    public Angle AngularVelocity;
    public TimeSpan LastUpdate;

    public SolarPanelComponentState(Angle angle, Angle angularVelocity, TimeSpan lastUpdate)
    {
        Angle = angle;
        AngularVelocity = angularVelocity;
        LastUpdate = lastUpdate;
    }
}
