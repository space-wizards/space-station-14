using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.CartridgeLoader.Cartridges;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NetProbeCartridgeComponent : Component
{
    /// <summary>
    /// The list of probed network devices
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProbedNetworkDevice> ProbedDevices = new();

    /// <summary>
    /// Limits the amount of devices that can be saved
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxSavedDevices = 9;

    [DataField]
    public SoundSpecifier SoundScan = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg");
}


