using System.Linq;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Robust.Shared.Map.Events;

namespace Content.Shared.DeviceNetwork.Systems;

public sealed partial class DeviceListSystem : EntitySystem
{
    [Dependency] private NetworkConfiguratorSystem _configurator = default!;

    [Dependency] private EntityQuery<DeviceNetworkComponent> _deviceNetworkQuery = default!;
    [Dependency] private EntityQuery<DeviceListComponent> _deviceListQuery = default!;

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<DeviceListComponent> ent, ref ComponentShutdown args)
    {
        foreach (var conf in ent.Comp.Configurators)
        {
            _configurator.OnDeviceListShutdown(conf, ent);
        }

        foreach (var device in ent.Comp.Devices)
        {
            if (_deviceNetworkQuery.TryComp(device, out var comp))
                comp.DeviceLists.Remove(ent);
        }

        ent.Comp.Devices.Clear();
    }

    public IEnumerable<EntityUid> GetAllDevices(Entity<DeviceListComponent?> ent)
    {
        return !_deviceListQuery.Resolve(ent.Owner, ref ent.Comp) ? [] : ent.Comp.Devices;
    }

    /// <summary>
    /// Gets the given device list as a dictionary
    /// </summary>
    /// <remarks>
    /// If any entity in the device list is pre-map init, it will show the entity UID of the device instead.
    /// </remarks>
    public Dictionary<LocDeviceAddress, (EntityUid, string)> GetDeviceList(Entity<DeviceListComponent?> ent)
    {
        if (!_deviceListQuery.Resolve(ent.Owner, ref ent.Comp))
            return new Dictionary<LocDeviceAddress, (EntityUid, string)>();

        var devices = new Dictionary<LocDeviceAddress, (EntityUid, string)>(ent.Comp.Devices.Count);

        foreach (var deviceUid in ent.Comp.Devices)
        {
            if (!_deviceNetworkQuery.TryComp(deviceUid, out var deviceNet))
                continue;

            var address = MetaData(deviceUid).EntityLifeStage == EntityLifeStage.MapInitialized
                ? DeviceLocalizationHelpers.GetAddressFromId(deviceNet)
                : $"UID: {deviceUid.ToString()}";

            devices.Add(new LocDeviceAddress(deviceNet.Data.AddressId, deviceNet.Prefix), (deviceUid, address));
        }

        return devices;
    }

    /// <summary>
    /// Checks if the given address is present in a device list
    /// </summary>
    /// <param name="ent">The entity that has the device list that should be checked for the address</param>
    /// <param name="address">The address to check for</param>
    /// <returns>True if the address is present. False if not</returns>
    public bool ExistsInDeviceList(Entity<DeviceListComponent?> ent, DeviceAddress address)
    {
        var addresses = GetDeviceList(ent).Keys.Select(x => x.AddressId);
        return addresses.Contains(address);
    }

    /// <summary>
    /// Filters the broadcasts recipient list against the device list as either an allow or deny list depending on the components IsAllowList field
    /// </summary>
    [SubscribeLocalEvent]
    private void OnBeforeBroadcast(Entity<DeviceListComponent> ent, ref BeforeBroadcastAttemptEvent args)
    {
        var component = ent.Comp;
        //Don't filter anything if the device list is empty
        if (component.Devices.Count == 0)
        {
            if (component.IsAllowList)
                args.Cancelled = true;
            return;
        }

        var filteredRecipients = new HashSet<Device>(args.Recipients.Count);

        foreach (var recipient in args.Recipients)
        {
            if (component.Devices.Contains(recipient.Owner) == component.IsAllowList)
                filteredRecipients.Add(recipient);
        }

        args.ModifiedRecipients = filteredRecipients;
    }

    /// <summary>
    /// Filters incoming packets if that is enabled <see cref="OnBeforeBroadcast"/>
    /// </summary>
    [SubscribeLocalEvent]
    private void OnBeforePacketSent(Entity<DeviceListComponent> ent, ref BeforePacketSentEvent args)
    {
        if (ent.Comp.HandleIncomingPackets && ent.Comp.Devices.Contains(args.Sender) != ent.Comp.IsAllowList)
            args.Cancelled = true;
    }

    public void OnDeviceShutdown(Entity<DeviceListComponent?> list, Entity<DeviceNetworkComponent> device)
    {
        device.Comp.DeviceLists.Remove(list.Owner);
        if (!_deviceListQuery.Resolve(list.Owner, ref list.Comp))
            return;

        list.Comp.Devices.Remove(device);
        Dirty(list);
    }

    private readonly List<EntityUid> _toRemove = new();

    [SubscribeLocalEvent]
    private void OnMapSave(BeforeSerializationEvent ev)
    {
        var enumerator = AllEntityQuery<DeviceListComponent, TransformComponent>();
        while (enumerator.MoveNext(out var uid, out var device, out var xform))
        {
            if (!ev.MapIds.Contains(xform.MapID))
                continue;

            foreach (var ent in device.Devices)
            {
                if (TerminatingOrDeleted(ent))
                {
                    // Entity was deleted.
                    // TODO remove these on deletion instead of on-save.
                    _toRemove.Add(ent);
                    continue;
                }

                var linkedXform = Transform(ent);

                // This is assuming that **all** of the map is getting saved.
                // Which is not necessarily true.
                // AAAAAAAAAAAAAA
                if (ev.MapIds.Contains(linkedXform.MapID))
                    continue;

                _toRemove.Add(ent);
                // TODO full game saves.
                // when full saves are supported, this should instead add data to the BeforeSaveEvent informing the
                // saving system that this map (or null-space entity) also needs to be included in the save.
                Log.Error(
                    $"Saving a device list ({ToPrettyString(uid)}) that has a reference to an entity on another map ({ToPrettyString(ent)}). Removing entity from list.");
            }

            if (_toRemove.Count == 0)
                continue;

            var old = device.Devices.ToList();
            device.Devices.ExceptWith(_toRemove);
            var listEv = new DeviceListUpdateEvent(old, device.Devices.ToList());
            RaiseLocalEvent(uid, ref listEv);
            Dirty(uid, device);
            _toRemove.Clear();
        }
    }

    /// <summary>
    ///     Updates the device list stored on this entity.
    /// </summary>
    /// <param name="ent">The entity to update.</param>
    /// <param name="devices">The devices to store.</param>
    /// <param name="merge">Whether to merge or replace the devices stored.</param>
    public DeviceListUpdateResult UpdateDeviceList(Entity<DeviceListComponent?> ent, IEnumerable<EntityUid> devices, bool merge = false)
    {
        if (!_deviceListQuery.Resolve(ent.Owner, ref ent.Comp))
            return DeviceListUpdateResult.NoComponent;

        var list = devices.ToList();
        var newDevices = new HashSet<EntityUid>(list);

        if (merge)
            newDevices.UnionWith(ent.Comp.Devices);

        if (newDevices.Count > ent.Comp.DeviceLimit)
        {
            return DeviceListUpdateResult.TooManyDevices;
        }

        var oldDevices = ent.Comp.Devices.ToList();
        foreach (var device in oldDevices)
        {
            if (newDevices.Contains(device))
                continue;

            ent.Comp.Devices.Remove(device);
            if (_deviceNetworkQuery.TryComp(device, out var comp))
                comp.DeviceLists.Remove(ent);
        }

        foreach (var device in newDevices)
        {
            if (!_deviceNetworkQuery.TryComp(device, out var comp))
                continue;

            if (!ent.Comp.Devices.Add(device))
                continue;

            comp.DeviceLists.Add(ent);
        }

        var ev = new DeviceListUpdateEvent(oldDevices, list);
        RaiseLocalEvent(ent, ref ev);

        Dirty(ent);

        return DeviceListUpdateResult.UpdateOk;
    }
}
