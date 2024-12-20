using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.Power.Generator;
using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.DeviceLinking.Components;

/// <summary>
/// A power sensor checks the power network it's anchored to.
/// Has 2 ports for when it is charging or discharging. They should never both be high.
/// Requires <see cref="PowerSwitchableComponent"/> to function.
/// </summary>
[RegisterComponent, Access(typeof(PowerSensorSystem))]
public sealed partial class PowerSensorComponent : Component
{
    /// <summary>
    /// Whether to check the power network's input or output battery stats.
    /// Useful when working with SMESes where input and output can both be important.
    /// Or with APCs where there is no output and only input.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Output;

    /// <summary>
    /// Tool quality to use for switching between input and output.
    /// Cannot be pulsing since linking uses that.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<ToolQualityPrototype> SwitchQuality = "Screwing";

    /// <summary>
    /// Sound played when switching between input and output.
    /// </summary>
    [DataField]
    public SoundSpecifier SwitchSound = new SoundPathSpecifier("/Audio/Machines/lightswitch.ogg");

    /// <summary>
    /// Name of the port set when the network is charging power.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<SourcePortPrototype> ChargingPort = "PowerCharging";

    /// <summary>
    /// Name of the port set when the network is discharging power.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<SourcePortPrototype> DischargingPort = "PowerDischarging";

    /// <summary>
    /// How long to wait before checking the power network.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan CheckDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Time at which power will be checked.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextCheck = TimeSpan.Zero;

    /// <summary>
    /// Charge the network was at, at the last check.
    /// Charging/discharging is derived from this.
    /// </summary>
    [DataField]
    public float LastCharge;

    // Initial state
    [DataField]
    public bool ChargingState;

    [DataField]
    public bool DischargingState;
}
