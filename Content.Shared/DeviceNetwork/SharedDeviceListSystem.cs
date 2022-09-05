using System.Linq;
using Robust.Shared.GameStates;

namespace Content.Shared.DeviceNetwork;

public abstract class SharedDeviceListSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<DeviceListComponent, ComponentGetState>(GetDeviceListState);
        SubscribeLocalEvent<DeviceListComponent, ComponentHandleState>(HandleDeviceListState);
    }

    /// <summary>
    ///     Updates the device list stored on this entity.
    /// </summary>
    /// <param name="uid">The entity to update.</param>
    /// <param name="devices">The devices to store.</param>
    /// <param name="merge">Whether to merge or replace the devices stored.</param>
    /// <param name="deviceList">Device list component</param>
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

    public IEnumerable<EntityUid> GetAllDevices(EntityUid uid, DeviceListComponent? component = null)
    {
        if (!Resolve(uid, ref component))
        {
            return new EntityUid[] { };
        }
        return component.Devices;
    }

    private void GetDeviceListState(EntityUid uid, DeviceListComponent comp, ref ComponentGetState args)
    {
        args.State = new DeviceListComponentState(comp.Devices, comp.IsAllowList, comp.HandleIncomingPackets);
    }

    private void HandleDeviceListState(EntityUid uid, DeviceListComponent comp, ref ComponentHandleState args)
    {
        if (args.Current is not DeviceListComponentState state)
        {
            return;
        }

        comp.Devices = state.Devices;
        comp.HandleIncomingPackets = state.HandleIncomingPackets;
        comp.IsAllowList = state.IsAllowList;
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
