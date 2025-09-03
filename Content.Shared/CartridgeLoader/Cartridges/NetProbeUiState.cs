using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class NetProbeUiState : BoundUserInterfaceState
{
    /// <summary>
    /// The list of probed network devices
    /// </summary>
    public List<ProbedNetworkDevice> ProbedDevices;

    public NetProbeUiState(List<ProbedNetworkDevice> probedDevices)
    {
        ProbedDevices = probedDevices;
    }
}

[Serializable, NetSerializable, DataRecord]
public sealed partial class ProbedNetworkDevice
{
    public readonly string Name;
    public readonly string Address;
    public readonly string Frequency;
    public readonly string NetId;

    public ProbedNetworkDevice(string name, string address, string frequency, string netId)
    {
        Name = name;
        Address = address;
        Frequency = frequency;
        NetId = netId;
    }
}
