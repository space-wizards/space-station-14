using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;
using SignalState = Content.Shared.DeviceLinking.Components.SignalState;

namespace Content.Server.DeviceLinking.Components;

/// <summary>
/// Server-side component for RNG device functionality containing server-only configuration data.
/// </summary>
[RegisterComponent]
public sealed partial class RngDeviceServerComponent : Component
{
    /// <summary>
    /// The input port that triggers the RNG roll.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> InputPort = "Trigger";

    /// <summary>
    /// Sound to play when the device is rolled.
    /// </summary>
    [DataField]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("Dice");

    /// <summary>
    /// Current signal state of the device.
    /// </summary>
    [DataField]
    public SignalState State = SignalState.Low;
}
