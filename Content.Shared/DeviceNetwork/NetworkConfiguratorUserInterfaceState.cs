using Robust.Shared.Serialization;

namespace Content.Shared.DeviceNetwork;

[Serializable, NetSerializable]
public sealed class NetworkConfiguratorUserInterfaceState : BoundUserInterfaceState
{
    public readonly HashSet<(string address, string name)> DeviceList;

    public NetworkConfiguratorUserInterfaceState(HashSet<(string, string)> deviceList)
    {
        DeviceList = deviceList;
    }
}
