using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.DeviceLinking.Components;

/// <summary>
///     Simple switch that will fire ports when toggled on or off. A button is jsut a switch that signals on the
///     same port regardless of its state.
/// </summary>
[RegisterComponent, Access(typeof(SignalSwitchSystem))]
public sealed partial class SignalSwitchComponent : Component
{
    /// <summary>
    ///     The port that gets signaled when the switch turns on.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> OnPort = "On";

    /// <summary>
    ///     The port that gets signaled when the switch turns off.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> OffPort = "Off";

    /// <summary>
    ///     The port that gets signaled with the switch's current status.
    ///     This is only used if OnPort is different from OffPort, not in the case of a toggle switch.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> StatusPort = "Status";

    [DataField("state")]
    public bool State;

    [DataField("clickSound")]
    public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/lightswitch.ogg");
}
