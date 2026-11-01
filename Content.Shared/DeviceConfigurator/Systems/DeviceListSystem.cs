using Content.Shared.DeviceConfigurator.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using JetBrains.Annotations;
using Robust.Shared.Map.Events;
using Robust.Shared.Utility;

namespace Content.Shared.DeviceConfigurator.Systems;

public sealed partial class DeviceListSystem : EntitySystem
{
    [Dependency] private NetworkConfiguratorSystem _configurator = default!;

    [Dependency] private EntityQuery<LinkedDeviceNetworkComponent> _linkedDeviceQuery;
    [Dependency] private EntityQuery<DeviceNetworkComponent> _deviceNetworkQuery;
    [Dependency] private EntityQuery<DeviceListComponent> _deviceListQuery;

    private readonly List<EntityUid> _toRemove = new();

    private readonly List<OptionDeviceAddress> _deviceCache = new();

    private readonly HashSet<EntityUid> _newCache = new();
    private readonly HashSet<EntityUid> _oldCache = new();

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<DeviceListComponent> ent, ref ComponentShutdown args)
    {
        foreach (var conf in ent.Comp.Configurators)
        {
            _configurator.OnDeviceListShutdown(conf, ent);
        }

        foreach (var device in ent.Comp.Devices)
        {
            if (!_linkedDeviceQuery.TryComp(device, out var comp))
                continue;

            comp.DeviceLists.Remove(ent);
            DirtyField(device, comp, nameof(LinkedDeviceNetworkComponent.DeviceLists));
        }

        ent.Comp.Devices.Clear();
    }

    public IEnumerable<EntityUid> GetAllDevices(Entity<DeviceListComponent?> ent)
    {
        return !_deviceListQuery.Resolve(ent.Owner, ref ent.Comp) ? [] : ent.Comp.Devices;
    }

    /// <summary>
    /// Gets the given device list as a dictionary.
    /// </summary>
    /// <remarks>
    /// If any entity in the device list is pre-map init, it will show the entity UID of the device instead.
    /// Doesn't work correctly for pre-init entities!
    /// </remarks>
    [PublicAPI]
    [Obsolete($"Use {nameof(List<>)} of {nameof(OptionDeviceAddress)} method overload instead")]
    public Dictionary<LocDeviceAddress, (EntityUid, string)> GetDeviceList(Entity<DeviceListComponent?> ent)
    {
        // The refactor apparently broke this method entirely...
        // So I'll just throw a debug assert when it is used in a wrong way.
        // Please forgive me for my sins against downstreams.
        DebugTools.Assert(MetaData(ent.Owner).EntityLifeStage < EntityLifeStage.MapInitialized,
            "GetDeviceList method dictionary variation no longer works correctly for non map-init entities." +
            "Please update your code to use a newer method");

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

            devices.Add(new LocDeviceAddress(deviceNet.Address, deviceNet.Prefix), (deviceUid, address));
        }

        return devices;
    }

    /// <summary>
    /// Gets the given device list.
    /// <para>
    /// If any entity in the device list is pre-map init, the <see cref="OptionDeviceAddress"/>
    /// will hold only <see cref="EntityUid"/> of the device list entities.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Remember to clear the provided <paramref name="list"/>
    /// manually before calling this method if you decide to reuse the same list.
    /// </remarks>
    /// <param name="ent">Entity to get the device list from.</param>
    /// <param name="list">A clean list of option device addresses, cached for better performance.</param>
    [PublicAPI]
    public void GetDeviceList(Entity<DeviceListComponent?> ent, List<OptionDeviceAddress> list)
    {
        if (!_deviceListQuery.Resolve(ent.Owner, ref ent.Comp))
            return;

        foreach (var deviceUid in ent.Comp.Devices)
        {
            if (!_deviceNetworkQuery.TryComp(deviceUid, out var deviceNet))
                continue;

            var option = MetaData(deviceUid).EntityLifeStage == EntityLifeStage.MapInitialized
                ? new OptionDeviceAddress(new LocDeviceAddress(deviceNet.Address, deviceNet.Prefix), deviceUid)
                : new OptionDeviceAddress(deviceUid);

            list.Add(option);
        }
    }

    /// <summary>
    /// Checks if the given address is present in a device list
    /// </summary>
    /// <param name="ent">The entity that has the device list that should be checked for the address</param>
    /// <param name="address">The address to check for</param>
    /// <returns>True if the address is present. False if not</returns>
    [PublicAPI]
    public bool ExistsInDeviceList(Entity<DeviceListComponent?> ent, DeviceAddress address)
    {
        _deviceCache.Clear();
        GetDeviceList(ent, _deviceCache);
        foreach (var device in _deviceCache)
        {
            if (!device.TryGetAddress(out var deviceAddress)
                || deviceAddress.Value.AddressId != address)
                continue;

            return true;
        }

        return false;
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

    public void OnDeviceShutdown(Entity<DeviceListComponent?> list, Entity<LinkedDeviceNetworkComponent> device)
    {
        device.Comp.DeviceLists.Remove(list.Owner);
        if (!_deviceListQuery.Resolve(list.Owner, ref list.Comp))
            return;

        list.Comp.Devices.Remove(device);
        Dirty(list);
    }

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

            _oldCache.Clear();
            _oldCache.UnionWith(device.Devices);
            device.Devices.ExceptWith(_toRemove);
            _toRemove.Clear();

            var listEv = new DeviceListUpdateEvent(_oldCache, device.Devices);
            RaiseLocalEvent(uid, ref listEv);
            DirtyField(uid, device, nameof(DeviceListComponent.Devices));
        }
    }

    /// <summary>
    /// Updates the device list stored on this entity.
    /// </summary>
    /// <param name="ent">The entity to update.</param>
    /// <param name="devices">The devices to store.</param>
    /// <param name="merge">Whether to merge or replace the devices stored.</param>
    public DeviceListUpdateResult UpdateDeviceList(Entity<DeviceListComponent?> ent, ReadOnlySpan<EntityUid> devices, bool merge = false)
    {
        if (!_deviceListQuery.Resolve(ent.Owner, ref ent.Comp))
            return DeviceListUpdateResult.NoComponent;

        _newCache.Clear();
        _oldCache.Clear();

        foreach (var device in devices)
        {
            _newCache.Add(device);
        }

        if (merge)
            _newCache.UnionWith(ent.Comp.Devices);

        if (_newCache.Count > ent.Comp.DeviceLimit)
        {
            return DeviceListUpdateResult.TooManyDevices;
        }

        _oldCache.Clear();
        _oldCache.UnionWith(ent.Comp.Devices);

        foreach (var device in _oldCache)
        {
            if (_newCache.Contains(device))
                continue;

            ent.Comp.Devices.Remove(device);

            if (!_linkedDeviceQuery.TryComp(device, out var comp))
                continue;

            comp.DeviceLists.Remove(ent);
            DirtyField(device, comp, nameof(LinkedDeviceNetworkComponent.DeviceLists));
        }

        foreach (var device in _newCache)
        {
            if (!_linkedDeviceQuery.TryComp(device, out var comp))
                continue;

            if (!ent.Comp.Devices.Add(device))
                continue;

            comp.DeviceLists.Add(ent);
            DirtyField(device, comp, nameof(LinkedDeviceNetworkComponent.DeviceLists));
        }

        var ev = new DeviceListUpdateEvent(_oldCache, ent.Comp.Devices);
        RaiseLocalEvent(ent, ref ev);

        DirtyField(ent, nameof(DeviceListComponent.Devices));

        return DeviceListUpdateResult.UpdateOk;
    }
}
