using Content.Server.Radio.EntitySystems;
using Content.Shared.Radio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Radio.Components;

/// <summary>
///     Listens for local chat messages and relays them to some radio frequency
/// </summary>
[RegisterComponent]
[Access(typeof(RadioDeviceSystem))]
public sealed class RadioMicrophoneComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("broadcastChannel", customTypeSerializer: typeof(PrototypeIdSerializer<RadioChannelPrototype>))]
    public string BroadcastChannel = "Common";

    [ViewVariables, DataField("supportedChannels", customTypeSerializer: typeof(PrototypeIdListSerializer<RadioChannelPrototype>))]
    public List<string>? SupportedChannels;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("listenRange")]
    public int ListenRange  = 4;

    [DataField("enabled")]
    public bool Enabled = false;

    [DataField("powerRequired")]
    public bool PowerRequired = false;

    /// <summary>
    /// Whether or not the speaker must have an
    /// unobstructed path to the radio to speak
    /// </summary>
    [DataField("unobstructedRequired")]
    public bool UnobstructedRequired = false;
}
