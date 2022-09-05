using System.Linq;

namespace Content.Shared.DeviceNetwork;

public abstract class SharedDeviceListSystem : EntitySystem
{
    public void UpdateDeviceList(EntityUid uid, IEnumerable<EntityUid> devices, bool merge = false, DeviceListComponent? deviceList = null)
    {
        if (!Resolve(uid, ref deviceList))
            return;

        if (!merge)
            deviceList.Devices.Clear();

        var devicesList = devices.ToList();
        deviceList.Devices.UnionWith(devicesList);

        RaiseLocalEvent(uid, new DeviceListUpdateEvent(devicesList));

        Dirty(deviceList);
    }
}

public sealed class DeviceListUpdateEvent : EntityEventArgs
{
    public DeviceListUpdateEvent(List<EntityUid> devices)
    {
        Devices = devices;
    }

    public List<EntityUid> Devices { get; }
}
