using System.Linq;
using Content.Server.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Systems;
using Content.Shared.Interaction;
using JetBrains.Annotations;
using Robust.Shared.Map.Events;

namespace Content.Server.DeviceNetwork.Systems;

[UsedImplicitly]
public sealed class DeviceListSystem : SharedDeviceListSystem
{
    private ISawmill _sawmill = default!;

    [Dependency] private DeviceNetworkSystem _deviceNetworkSystem = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DeviceListComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<DeviceListComponent, BeforeBroadcastAttemptEvent>(OnBeforeBroadcast);
        SubscribeLocalEvent<DeviceListComponent, BeforePacketSentEvent>(OnBeforePacketSent);
        SubscribeLocalEvent<DeviceListComponent, DeviceShutDownEvent>(OnDeviceShutdown);
        SubscribeLocalEvent<BeforeSaveEvent>(OnMapSave);
        _sawmill = Logger.GetSawmill("devicelist");
    }

    public void OnInit(EntityUid uid, DeviceListComponent component, ComponentInit args)
    {
        Dirty(component);
    }

    /// <summary>
    /// Gets the given device list as a dictionary
    /// </summary>
    /// <remarks>
    /// If any entity in the device list is pre-map init, it will show the entity UID of the device instead.
    /// </remarks>
    public Dictionary<string, EntityUid> GetDeviceList(EntityUid uid, DeviceListComponent? deviceList = null)
    {
        if (!Resolve(uid, ref deviceList))
            return new Dictionary<string, EntityUid>();

        var devices = new Dictionary<string, EntityUid>(deviceList.Devices.Count);

        foreach (var deviceUid in deviceList.Devices)
        {
            if (!TryComp(deviceUid, out DeviceNetworkComponent? deviceNet))
                continue;

            var address = MetaData(deviceUid).EntityLifeStage == EntityLifeStage.MapInitialized
                ? deviceNet.Address
                : $"UID: {deviceUid.ToString()}";

            devices.Add(address, deviceUid);

        }

        return devices;
    }

    /// <summary>
    /// Checks if the given address is present in a device list
    /// </summary>
    /// <param name="uid">The entity uid that has the device list that should be checked for the address</param>
    /// <param name="address">The address to check for</param>
    /// <param name="deviceList">The device list component</param>
    /// <returns>True if the address is present. False if not</returns>
    public bool ExistsInDeviceList(EntityUid uid, string address, DeviceListComponent? deviceList = null)
    {
        var addresses = GetDeviceList(uid).Keys;
        return addresses.Contains(address);
    }

    protected override void UpdateShutdownSubscription(EntityUid uid, List<EntityUid> newDevices, List<EntityUid> oldDevices)
    {
        foreach (var device in newDevices)
        {
            _deviceNetworkSystem.SubscribeToDeviceShutdown(uid, device);
        }

        var removedDevices = oldDevices.Except(newDevices);
        foreach (var device in removedDevices)
        {
            _deviceNetworkSystem.UnsubscribeFromDeviceShutdown(uid, device);
        }
    }

    /// <summary>
    /// Filters the broadcasts recipient list against the device list as either an allow or deny list depending on the components IsAllowList field
    /// </summary>
    private void OnBeforeBroadcast(EntityUid uid, DeviceListComponent component, BeforeBroadcastAttemptEvent args)
    {
        //Don't filter anything if the device list is empty
        if (component.Devices.Count == 0)
        {
            if (component.IsAllowList) args.Cancel();
            return;
        }

        HashSet<DeviceNetworkComponent> filteredRecipients = new(args.Recipients.Count);

        foreach (var recipient in args.Recipients)
        {
            if (component.Devices.Contains(recipient.Owner) == component.IsAllowList) filteredRecipients.Add(recipient);
        }

        args.ModifiedRecipients = filteredRecipients;
    }

    /// <summary>
    /// Filters incoming packets if that is enabled <see cref="OnBeforeBroadcast"/>
    /// </summary>
    private void OnBeforePacketSent(EntityUid uid, DeviceListComponent component, BeforePacketSentEvent args)
    {
        if (component.HandleIncomingPackets && component.Devices.Contains(args.Sender) != component.IsAllowList)
            args.Cancel();
    }

    private void OnDeviceShutdown(EntityUid uid, DeviceListComponent component, ref DeviceShutDownEvent args)
    {
        component.Devices.Remove(args.ShutDownEntityUid);
        Dirty(component);
    }

    private void OnMapSave(BeforeSaveEvent ev)
    {
        List<EntityUid> toRemove = new();
        var query = GetEntityQuery<TransformComponent>();
        var enumerator = AllEntityQuery<DeviceListComponent, TransformComponent>();
        while (enumerator.MoveNext(out var uid, out var device, out var xform))
        {
            if (xform.MapUid != ev.Map)
                continue;

            foreach (var ent in device.Devices)
            {
                if (!query.TryGetComponent(ent, out var linkedXform))
                {
                    // Entity was deleted.
                    // TODO remove these on deletion instead of on-save.
                    toRemove.Add(ent);
                    continue;
                }

                if (linkedXform.MapUid == ev.Map)
                    continue;

                toRemove.Add(ent);
                // TODO full game saves.
                // when full saves are supported, this should instead add data to the BeforeSaveEvent informing the
                // saving system that this map (or null-space entity) also needs to be included in the save.
                _sawmill.Error(
                    $"Saving a device list ({ToPrettyString(uid)}) that has a reference to an entity on another map ({ToPrettyString(ent)}). Removing entity from list.");
            }

            if (toRemove.Count == 0)
                continue;

            var old = device.Devices.ToList();
            device.Devices.ExceptWith(toRemove);
            RaiseLocalEvent(uid, new DeviceListUpdateEvent(old, device.Devices.ToList()));
            Dirty(device);
            toRemove.Clear();
        }
    }
}
