using Robust.Shared.Audio;

namespace Content.Shared.CartridgeLoader.Cartridges;

[RegisterComponent]
public sealed partial class NetProbeCartridgeComponent : Component
{
    /// <summary>
    /// The list of probed network devices
    /// </summary>
    [DataField("probedDevices")]
    public List<ProbedNetworkDevice> ProbedDevices = new();

    /// <summary>
    /// Limits the amount of devices that can be saved
    /// </summary>
    [DataField("maxSavedDevices")]
    public int MaxSavedDevices { get; set; } = 9;

    [DataField("soundScan")]
    public SoundSpecifier SoundScan = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg");
}


