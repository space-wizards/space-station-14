using System.Linq;
using Content.Shared.Database;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeviceNetwork.Systems;

public sealed partial class NetworkConfiguratorSystem
{
    private void OpenDeviceLinkUi(Entity<NetworkConfiguratorComponent> configurator, EntityUid? targetUid, EntityUid userUid)
    {
        if (Delay(configurator))
            return;

        if (!targetUid.HasValue || !configurator.Comp.ActiveDeviceLink.HasValue || !AccessCheck(targetUid.Value, userUid, configurator))
            return;

        _uiSystem.OpenUi(configurator.Owner, NetworkConfiguratorUiKey.Link, userUid);
        configurator.Comp.DeviceLinkTarget = targetUid;
        DirtyField(configurator.AsNullable(), nameof(NetworkConfiguratorComponent.DeviceLinkTarget));

        if (_deviceLinkSourceQuery.TryComp(configurator.Comp.ActiveDeviceLink, out var activeSource)
            && _deviceLinkSinkQuery.TryComp(targetUid, out var targetSink))
        {
            UpdateLinkUiState(configurator, (configurator.Comp.ActiveDeviceLink.Value, activeSource), (targetUid.Value, targetSink));
        }
        else if (_deviceLinkSinkQuery.TryComp(configurator.Comp.ActiveDeviceLink, out var activeSink)
                 && _deviceLinkSourceQuery.TryComp(targetUid, out var targetSource))
        {
            UpdateLinkUiState(configurator, (targetUid.Value, targetSource), (configurator.Comp.ActiveDeviceLink.Value, activeSink));
        }
    }

    private void UpdateLinkUiState(
        Entity<NetworkConfiguratorComponent> configurator,
        Entity<DeviceLinkSourceComponent?, DeviceNetworkComponent?> source,
        Entity<DeviceLinkSinkComponent?, DeviceNetworkComponent?> sink)
    {
        if (_deviceLinkSourceQuery.Resolve(source.Owner, ref source.Comp1, false)
            || _deviceLinkSinkQuery.Resolve(sink.Owner, ref sink.Comp1, false))
            return;

        var sources = _deviceLinkSystem.GetSourcePorts(source);
        var sinks = _deviceLinkSystem.GetSinkPortIds(sink!);
        var links = _deviceLinkSystem.GetLinks(source, sink);
        var defaults = _deviceLinkSystem.GetDefaults(sources);
        var sourceIds = sources.Select(s => (ProtoId<SourcePortPrototype>)s.ID).ToArray();

        var sourceAddress = string.Empty;
        var sinkAddress = string.Empty;
        var sourceAddressId = DeviceAddress.Invalid;
        var sinkAddressId = DeviceAddress.Invalid;
        if (_deviceNetworkQuery.Resolve(source.Owner, ref source.Comp2, false))
        {
            sourceAddress = _deviceNetwork.GetAddress((source.Owner, source.Comp2));
            sourceAddressId = source.Comp2.Data.AddressId;
        }
        if (_deviceNetworkQuery.Resolve(sink.Owner, ref sink.Comp2, false))
        {
            sinkAddress = _deviceNetwork.GetAddress((sink.Owner, sink.Comp2));
            sinkAddressId = sink.Comp2.Data.AddressId;
        }

        var state = new DeviceLinkUserInterfaceState(
            sourceIds,
            sinks,
            links,
            sourceAddressId,
            sinkAddressId,
            sourceAddress,
            sinkAddress,
            defaults);
        _uiSystem.SetUiState(configurator.Owner, NetworkConfiguratorUiKey.Link, state);
    }

    /// <summary>
    /// Opens the config ui. It can be used to modify the devices in the targets device list.
    /// </summary>
    private void OpenDeviceListUi(Entity<NetworkConfiguratorComponent> configurator, EntityUid? targetUid, EntityUid userUid)
    {
        if (configurator.Comp.ActiveDeviceLink == targetUid)
            return;

        if (Delay(configurator))
            return;

        if (!targetUid.HasValue || !AccessCheck(targetUid.Value, userUid, configurator))
            return;

        if (!_deviceListQuery.TryComp(targetUid, out var list))
            return;

        if (_deviceListQuery.TryComp(configurator.Comp.ActiveDeviceList, out var oldList))
        {
            oldList.Configurators.Remove(configurator);
            DirtyField(configurator.Comp.ActiveDeviceList.Value, oldList, nameof(DeviceListComponent.Configurators));
        }

        list.Configurators.Add(configurator);
        configurator.Comp.ActiveDeviceList = targetUid;
        DirtyField(configurator.AsNullable(), nameof(NetworkConfiguratorComponent.ActiveDeviceList));
        DirtyField(targetUid.Value, list, nameof(DeviceListComponent.Configurators));

        if (_uiSystem.TryOpenUi(configurator.Owner, NetworkConfiguratorUiKey.Configure, userUid))
        {
            _uiSystem.SetUiState(configurator.Owner,
                NetworkConfiguratorUiKey.Configure,
                new DeviceListUserInterfaceState(
                _deviceListSystem.GetDeviceList(configurator.Comp.ActiveDeviceList.Value)
                    .Select(v => (v.Key, MetaData(v.Value.Item1).EntityName))
                    .ToHashSet()
            ));
        }
    }

    /// <summary>
    /// Sends the list of saved devices to the ui
    /// </summary>
    private void UpdateListUiState(Entity<NetworkConfiguratorComponent> ent)
    {
        HashSet<(LocDeviceAddress address, string name)> devices = new();
        HashSet<DeviceAddress> invalidDevices = new();

        foreach (var pair in ent.Comp.Devices)
        {
            if (!Exists(pair.Value)
                || !_deviceNetworkQuery.TryComp(pair.Value, out var deviceComp))
            {
                invalidDevices.Add(pair.Key);
                continue;
            }

            devices.Add(((pair.Key, deviceComp.Prefix), Name(pair.Value)));
        }

        //Remove saved entities that don't exist anymore
        foreach (var invalidDevice in invalidDevices)
        {
            ent.Comp.Devices.Remove(invalidDevice);
        }

        DirtyField(ent.AsNullable(), nameof(NetworkConfiguratorComponent.Devices));
        _uiSystem.SetUiState(ent.Owner, NetworkConfiguratorUiKey.List, new NetworkConfiguratorUserInterfaceState(devices));
    }

    /// <summary>
    /// Clears the active device list when the ui is closed
    /// </summary>
    [SubscribeLocalEvent]
    private void OnUiClosed(Entity<NetworkConfiguratorComponent> ent, ref BoundUIClosedEvent args)
    {
        if (!args.UiKey.Equals(NetworkConfiguratorUiKey.Configure)
            && !args.UiKey.Equals(NetworkConfiguratorUiKey.Link)
            && !args.UiKey.Equals(NetworkConfiguratorUiKey.List))
        {
            return;
        }

        if (_deviceListQuery.TryComp(ent.Comp.ActiveDeviceList, out var list))
        {
            list.Configurators.Remove(ent);
            DirtyField(ent.Comp.ActiveDeviceList.Value, list, nameof(DeviceListComponent.Configurators));
        }

        ent.Comp.ActiveDeviceList = null;

        if (args.UiKey is NetworkConfiguratorUiKey.Link)
        {
            ent.Comp.ActiveDeviceLink = null;
            ent.Comp.DeviceLinkTarget = null;
            DirtyField(ent.AsNullable(), nameof(NetworkConfiguratorComponent.ActiveDeviceLink));
            DirtyField(ent.AsNullable(), nameof(NetworkConfiguratorComponent.DeviceLinkTarget));
        }

        DirtyField(ent.AsNullable(), nameof(NetworkConfiguratorComponent.ActiveDeviceList));
    }

    public void OnDeviceListShutdown(Entity<NetworkConfiguratorComponent?> conf, Entity<DeviceListComponent> list)
    {
        list.Comp.Configurators.Remove(conf.Owner);
        if (_networkConfigQuery.Resolve(conf.Owner, ref conf.Comp))
            conf.Comp.ActiveDeviceList = null;

        DirtyField(list.AsNullable(), nameof(DeviceListComponent.Configurators));
        DirtyField(conf, nameof(NetworkConfiguratorComponent.ActiveDeviceList));
    }

    /// <summary>
    /// Removes a device from the saved devices list
    /// </summary>
    [SubscribeLocalEvent]
    private void OnRemoveDevice(Entity<NetworkConfiguratorComponent> ent, ref NetworkConfiguratorRemoveDeviceMessage args)
    {
        if (ent.Comp.Devices.TryGetValue(args.Address.AddressId, out var removedDevice))
        {
            _adminLogger.Add(LogType.DeviceLinking,
                LogImpact.Low,
                $"{ToPrettyString(args.Actor):actor} removed buffered device {ToPrettyString(removedDevice):subject} from {ToPrettyString(ent):tool}");
        }

        ent.Comp.Devices.Remove(args.Address.AddressId);
        if (_deviceNetworkQuery.TryComp(removedDevice, out var device))
        {
            device.Configurators.Remove(ent);
            DirtyField(removedDevice, device, nameof(DeviceNetworkComponent.Configurators));
        }

        UpdateListUiState(ent);
        DirtyField(ent.AsNullable(), nameof(NetworkConfiguratorComponent.Devices));
    }

    [SubscribeLocalEvent]
    private void OnClearDevice(Entity<NetworkConfiguratorComponent> ent, ref NetworkConfiguratorClearDevicesMessage args)
    {
        _adminLogger.Add(LogType.DeviceLinking,
            LogImpact.Low,
            $"{ToPrettyString(args.Actor):actor} cleared buffered devices from {ToPrettyString(ent):tool}");

        ClearDevices(ent);
        UpdateListUiState(ent);
    }

    private void ClearDevices(Entity<NetworkConfiguratorComponent> ent)
    {
        foreach (var device in ent.Comp.Devices.Values)
        {
            if (!_deviceNetworkQuery.TryComp(device, out var comp))
                continue;

            comp.Configurators.Remove(ent);
            DirtyField(device, comp, nameof(DeviceNetworkComponent.Configurators));
        }

        ent.Comp.Devices.Clear();
        DirtyField(ent.AsNullable(), nameof(NetworkConfiguratorComponent.Devices));
    }

    [SubscribeLocalEvent]
    private void OnClearLinks(Entity<NetworkConfiguratorComponent> ent, ref NetworkConfiguratorClearLinksMessage args)
    {
        if (!ent.Comp.ActiveDeviceLink.HasValue || !ent.Comp.DeviceLinkTarget.HasValue)
            return;

        _adminLogger.Add(LogType.DeviceLinking,
            LogImpact.Low,
            $"{ToPrettyString(args.Actor):actor} cleared links between {ToPrettyString(ent.Comp.ActiveDeviceLink.Value):subject} and {ToPrettyString(ent.Comp.DeviceLinkTarget.Value):subject2} with {ToPrettyString(ent):tool}");

        if (_deviceLinkSourceQuery.HasComp(ent.Comp.ActiveDeviceLink)
            && _deviceLinkSinkQuery.HasComp(ent.Comp.DeviceLinkTarget))
        {
            _deviceLinkSystem.RemoveSinkFromSource(
                ent.Comp.ActiveDeviceLink.Value,
                ent.Comp.DeviceLinkTarget.Value);

            UpdateLinkUiState(
                ent,
                ent.Comp.ActiveDeviceLink.Value,
                ent.Comp.DeviceLinkTarget.Value);
        }
        else if (_deviceLinkSourceQuery.HasComp(ent.Comp.DeviceLinkTarget)
                 && _deviceLinkSinkQuery.HasComp(ent.Comp.ActiveDeviceLink))
        {
            _deviceLinkSystem.RemoveSinkFromSource(
                ent.Comp.DeviceLinkTarget.Value,
                ent.Comp.ActiveDeviceLink.Value);

            UpdateLinkUiState(
                ent,
                ent.Comp.DeviceLinkTarget.Value,
                ent.Comp.ActiveDeviceLink.Value);
        }
    }

    [SubscribeLocalEvent]
    private void OnToggleLinks(Entity<NetworkConfiguratorComponent> ent, ref NetworkConfiguratorToggleLinkMessage args)
    {
        if (!ent.Comp.ActiveDeviceLink.HasValue || !ent.Comp.DeviceLinkTarget.HasValue)
            return;

        if (_deviceLinkSourceQuery.TryComp(ent.Comp.ActiveDeviceLink, out var activeSource)
            && _deviceLinkSinkQuery.TryComp(ent.Comp.DeviceLinkTarget, out var targetSink))
        {
            _deviceLinkSystem.ToggleLink(
                args.Actor,
                (ent.Comp.ActiveDeviceLink.Value, activeSource),
                (ent.Comp.DeviceLinkTarget.Value, targetSink),
                args.Link);

            UpdateLinkUiState(ent, (ent.Comp.ActiveDeviceLink.Value, activeSource), ent.Comp.DeviceLinkTarget.Value);
        }
        else if (_deviceLinkSourceQuery.TryComp(ent.Comp.DeviceLinkTarget, out var targetSource)
                 && _deviceLinkSinkQuery.TryComp(ent.Comp.ActiveDeviceLink, out var activeSink))
        {
            _deviceLinkSystem.ToggleLink(
                args.Actor,
                (ent.Comp.DeviceLinkTarget.Value, targetSource),
                (ent.Comp.ActiveDeviceLink.Value, activeSink),
                args.Link);

            UpdateLinkUiState(
                ent,
                (ent.Comp.DeviceLinkTarget.Value, targetSource),
                ent.Comp.ActiveDeviceLink.Value);
        }
    }

    /// <summary>
    /// Saves links set by the device link UI
    /// </summary>
    [SubscribeLocalEvent]
    private void OnSaveLinks(Entity<NetworkConfiguratorComponent> ent, ref NetworkConfiguratorLinksSaveMessage args)
    {
        if (!ent.Comp.ActiveDeviceLink.HasValue || !ent.Comp.DeviceLinkTarget.HasValue)
            return;

        if (_deviceLinkSourceQuery.TryComp(ent.Comp.ActiveDeviceLink, out var activeSource)
            && _deviceLinkSinkQuery.TryComp(ent.Comp.DeviceLinkTarget, out var targetSink))
        {
            _deviceLinkSystem.SaveLinks(
                args.Actor,
                (ent.Comp.ActiveDeviceLink.Value, activeSource),
                (ent.Comp.DeviceLinkTarget.Value, targetSink),
                args.Links);

            UpdateLinkUiState(
                ent,
                (ent.Comp.ActiveDeviceLink.Value, activeSource),
                ent.Comp.DeviceLinkTarget.Value);
        }
        else if (_deviceLinkSourceQuery.TryComp(ent.Comp.DeviceLinkTarget, out var targetSource)
                 && _deviceLinkSinkQuery.TryComp(ent.Comp.ActiveDeviceLink, out var activeSink))
        {
            _deviceLinkSystem.SaveLinks(
                args.Actor,
                (ent.Comp.DeviceLinkTarget.Value, targetSource),
                (ent.Comp.ActiveDeviceLink.Value, activeSink),
                args.Links);

            UpdateLinkUiState(
                ent,
                (ent.Comp.DeviceLinkTarget.Value, targetSource),
                ent.Comp.ActiveDeviceLink.Value);
        }
    }

    /// <summary>
    /// Handles all the button presses from the config ui.
    /// Modifies, copies or visualizes the targets device list
    /// </summary>
    [SubscribeLocalEvent]
    private void OnConfigButtonPressed(Entity<NetworkConfiguratorComponent> ent, ref NetworkConfiguratorButtonPressedMessage args)
    {
        if (!ent.Comp.ActiveDeviceList.HasValue)
            return;

        var result = DeviceListUpdateResult.NoComponent;
        switch (args.ButtonKey)
        {
            case NetworkConfiguratorButtonKey.Set:
                _adminLogger.Add(LogType.DeviceLinking,
                    LogImpact.Low,
                    $"{ToPrettyString(args.Actor):actor} set device links to {ToPrettyString(ent.Comp.ActiveDeviceList.Value):subject} with {ToPrettyString(ent):tool}");

                result = _deviceListSystem.UpdateDeviceList(ent.Comp.ActiveDeviceList.Value, new HashSet<EntityUid>(ent.Comp.Devices.Values));
                break;
            case NetworkConfiguratorButtonKey.Add:
                _adminLogger.Add(LogType.DeviceLinking,
                    LogImpact.Low,
                    $"{ToPrettyString(args.Actor):actor} added device links to {ToPrettyString(ent.Comp.ActiveDeviceList.Value):subject} with {ToPrettyString(ent):tool}");

                result = _deviceListSystem.UpdateDeviceList(ent.Comp.ActiveDeviceList.Value, new HashSet<EntityUid>(ent.Comp.Devices.Values), true);
                break;
            case NetworkConfiguratorButtonKey.Clear:
                _adminLogger.Add(LogType.DeviceLinking,
                    LogImpact.Low,
                    $"{ToPrettyString(args.Actor):actor} cleared device links from {ToPrettyString(ent.Comp.ActiveDeviceList.Value):subject} with {ToPrettyString(ent):tool}");
                result = _deviceListSystem.UpdateDeviceList(ent.Comp.ActiveDeviceList.Value, new HashSet<EntityUid>());
                break;
            case NetworkConfiguratorButtonKey.Copy:
                _adminLogger.Add(LogType.DeviceLinking,
                    LogImpact.Low,
                    $"{ToPrettyString(args.Actor):actor} copied devices from {ToPrettyString(ent.Comp.ActiveDeviceList.Value):subject} to {ToPrettyString(ent):tool}");

                ClearDevices(ent);

                foreach (var (addr, device) in _deviceListSystem.GetDeviceList(ent.Comp.ActiveDeviceList.Value))
                {
                    if (!_deviceNetworkQuery.TryComp(device.Item1, out var comp))
                        continue;

                    ent.Comp.Devices.Add(addr.AddressId, device.Item1);
                    comp.Configurators.Add(ent);
                }

                UpdateListUiState(ent);
                return;
            case NetworkConfiguratorButtonKey.Show:
                break;
        }

        var resultText = result switch
        {
            DeviceListUpdateResult.TooManyDevices => Loc.GetString("network-configurator-too-many-devices"),
            DeviceListUpdateResult.UpdateOk => Loc.GetString("network-configurator-update-ok"),
            _ => "error"
        };

        _popupSystem.PopupCursor(Loc.GetString(resultText), args.Actor, PopupType.Medium);
        _uiSystem.SetUiState(
            ent.Owner,
            NetworkConfiguratorUiKey.Configure,
            new DeviceListUserInterfaceState(
                _deviceListSystem.GetDeviceList(ent.Comp.ActiveDeviceList.Value)
                    .Select(v => (v.Key, MetaData(v.Value.Item1).EntityName))
                    .ToHashSet()));
    }

    public void OnDeviceShutdown(Entity<NetworkConfiguratorComponent?> conf, Entity<DeviceNetworkComponent> device)
    {
        device.Comp.Configurators.Remove(conf.Owner);
        DirtyField(device.AsNullable(), nameof(DeviceNetworkComponent.Configurators));

        if (!Resolve(conf.Owner, ref conf.Comp))
            return;

        foreach (var (addr, dev) in conf.Comp.Devices)
        {
            if (device.Owner != dev)
                continue;

            conf.Comp.Devices.Remove(addr);
            DirtyField(conf, nameof(NetworkConfiguratorComponent.Devices));
        }

        UpdateListUiState(conf!);
    }
}
