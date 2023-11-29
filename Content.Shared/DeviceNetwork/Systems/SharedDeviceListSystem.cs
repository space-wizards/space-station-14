using System.Linq;
using Content.Shared.DeviceNetwork.Components;

namespace Content.Shared.DeviceNetwork.Systems;

public abstract class SharedDeviceListSystem : EntitySystem
{
    /// <summary>
    ///     Updates the device list stored on this entity.
    /// </summary>
    /// <param name="uid">The entity to update.</param>
    /// <param name="devices">The devices to store.</param>
    /// <param name="merge">Whether to merge or replace the devices stored.</param>
    /// <param name="deviceList">Device list component</param>
    public DeviceListUpdateResult UpdateDeviceList(EntityUid uid, IEnumerable<EntityUid> devices, bool merge = false, DeviceListComponent? deviceList = null)
    {
        if (!Resolve(uid, ref deviceList))
            return DeviceListUpdateResult.NoComponent;

        var oldDevices = deviceList.Devices.ToList();
        var newDevices = merge ? new HashSet<EntityUid>(deviceList.Devices) : new();
        var devicesList = devices.ToList();

        newDevices.UnionWith(devicesList);
        if (newDevices.Count > deviceList.DeviceLimit)
        {
            return DeviceListUpdateResult.TooManyDevices;
        }

        deviceList.Devices = newDevices;

        UpdateShutdownSubscription(uid, devicesList, oldDevices);

        RaiseLocalEvent(uid, new DeviceListUpdateEvent(oldDevices, devicesList));

        Dirty(deviceList);

        return DeviceListUpdateResult.UpdateOk;
    }

    public IEnumerable<EntityUid> GetAllDevices(EntityUid uid, DeviceListComponent? component = null)
    {
        if (!Resolve(uid, ref component))
        {
            return new EntityUid[] { };
        }
        return component.Devices;
    }

    protected virtual void UpdateShutdownSubscription(EntityUid uid, List<EntityUid> devicesList, List<EntityUid> oldDevices)
    {
    }
}

public sealed class DeviceListUpdateEvent : EntityEventArgs
{
    public DeviceListUpdateEvent(List<EntityUid> oldDevices, List<EntityUid> devices)
    {
        OldDevices = oldDevices;
        Devices = devices;
    }

    public List<EntityUid> OldDevices { get; }
    public List<EntityUid> Devices { get; }
}

public enum DeviceListUpdateResult : byte
{
    NoComponent,
    TooManyDevices,
    UpdateOk
}
