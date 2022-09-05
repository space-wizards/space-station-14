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
