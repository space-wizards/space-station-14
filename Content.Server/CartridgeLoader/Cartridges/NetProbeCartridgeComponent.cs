using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Shared.Audio;

namespace Content.Server.CartridgeLoader.Cartridges;

[RegisterComponent]
public sealed class NetProbeCartridgeComponent : Component
{
    /// <summary>
    /// The list of probed network devices
    /// </summary>
    [DataField("probedDevices")]
    public List<ProbedNetworkDevice> ProbedDevices = new();

    [DataField("soundScan")]
    public SoundSpecifier SoundScan = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg");
}


