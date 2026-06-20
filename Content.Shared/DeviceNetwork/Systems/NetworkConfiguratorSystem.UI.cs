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
    private void InitializeUI()
    {
        SubscribeLocalEvent<NetworkConfiguratorComponent, BoundUIClosedEvent>(OnUiClosed);
        SubscribeLocalEvent<NetworkConfiguratorComponent, NetworkConfiguratorRemoveDeviceMessage>(OnRemoveDevice);
        SubscribeLocalEvent<NetworkConfiguratorComponent, NetworkConfiguratorClearDevicesMessage>(OnClearDevice);
        SubscribeLocalEvent<NetworkConfiguratorComponent, NetworkConfiguratorLinksSaveMessage>(OnSaveLinks);
        SubscribeLocalEvent<NetworkConfiguratorComponent, NetworkConfiguratorClearLinksMessage>(OnClearLinks);
        SubscribeLocalEvent<NetworkConfiguratorComponent, NetworkConfiguratorToggleLinkMessage>(OnToggleLinks);
        SubscribeLocalEvent<NetworkConfiguratorComponent, NetworkConfiguratorButtonPressedMessage>(OnConfigButtonPressed);
    }

    private void OpenDeviceLinkUi(Entity<NetworkConfiguratorComponent> configurator, EntityUid? targetUid, EntityUid userUid)
    {
        if (Delay(configurator))
            return;

        if (!targetUid.HasValue || !configurator.Comp.ActiveDeviceLink.HasValue || !AccessCheck(targetUid.Value, userUid, configurator))
            return;

        _uiSystem.OpenUi(configurator.Owner, NetworkConfiguratorUiKey.Link, userUid);
        configurator.Comp.DeviceLinkTarget = targetUid;

        if (TryComp(configurator.Comp.ActiveDeviceLink, out DeviceLinkSourceComponent? activeSource) && TryComp(targetUid, out DeviceLinkSinkComponent? targetSink))
        {
            UpdateLinkUiState(configurator, (configurator.Comp.ActiveDeviceLink.Value, activeSource), (targetUid.Value, targetSink));
        }
        else if (TryComp(configurator.Comp.ActiveDeviceLink, out DeviceLinkSinkComponent? activeSink)
                 && TryComp(targetUid, out DeviceLinkSourceComponent? targetSource))
        {
            UpdateLinkUiState(configurator, (targetUid.Value, targetSource), (configurator.Comp.ActiveDeviceLink.Value, activeSink));
        }
    }

    private void UpdateLinkUiState(
        EntityUid configuratorUid,
        Entity<DeviceLinkSourceComponent?, DeviceNetworkComponent?> source,
        Entity<DeviceLinkSinkComponent?, DeviceNetworkComponent?> sink)
    {
        if (!Resolve(source.Owner, ref source.Comp1, false) || !Resolve(sink.Owner, ref sink.Comp1, false))
            return;

        var sources = _deviceLinkSystem.GetSourcePorts(source);
        var sinks = _deviceLinkSystem.GetSinkPortIds(sink!);
        var links = _deviceLinkSystem.GetLinks(source, sink);
        var defaults = _deviceLinkSystem.GetDefaults(sources);
        var sourceIds = sources.Select(s => (ProtoId<SourcePortPrototype>)s.ID).ToArray();

        var sourceAddress = Resolve(source.Owner, ref source.Comp2, false) ? source.Comp2.Address : "";
        var sinkAddress = Resolve(sink.Owner, ref sink.Comp2, false) ? sink.Comp2.Address : "";

        var state = new DeviceLinkUserInterfaceState(sourceIds, sinks, links, sourceAddress, sinkAddress, defaults);
        _uiSystem.SetUiState(configuratorUid, NetworkConfiguratorUiKey.Link, state);
    }

    /// <summary>
    /// Opens the config ui. It can be used to modify the devices in the targets device list.
    /// </summary>
    private void OpenDeviceListUi(EntityUid configuratorUid, EntityUid? targetUid, EntityUid userUid, NetworkConfiguratorComponent configurator)
    {
        if (configurator.ActiveDeviceLink == targetUid)
            return;

        if (Delay(configurator))
            return;

        if (!targetUid.HasValue || !AccessCheck(targetUid.Value, userUid, configurator))
            return;

        if (!TryComp(targetUid, out DeviceListComponent? list))
            return;

        if (TryComp(configurator.ActiveDeviceList, out DeviceListComponent? oldList))
            oldList.Configurators.Remove(configuratorUid);

        list.Configurators.Add(configuratorUid);
        configurator.ActiveDeviceList = targetUid;
        Dirty(configuratorUid, configurator);

        if (_uiSystem.TryOpenUi(configuratorUid, NetworkConfiguratorUiKey.Configure, userUid))
        {
            _uiSystem.SetUiState(configuratorUid,
                NetworkConfiguratorUiKey.Configure,
                new DeviceListUserInterfaceState(
                _deviceListSystem.GetDeviceList(configurator.ActiveDeviceList.Value)
                    .Select(v => (v.Key, MetaData(v.Value).EntityName))
                    .ToHashSet()
            ));
        }
    }

    /// <summary>
    /// Sends the list of saved devices to the ui
    /// </summary>
    private void UpdateListUiState(Entity<NetworkConfiguratorComponent> ent)
    {
        HashSet<(string address, string name)> devices = new();
        HashSet<string> invalidDevices = new();

        foreach (var pair in ent.Comp.Devices)
        {
            if (!Exists(pair.Value))
            {
                invalidDevices.Add(pair.Key);
                continue;
            }

            devices.Add((pair.Key, Name(pair.Value)));
        }

        //Remove saved entities that don't exist anymore
        foreach (var invalidDevice in invalidDevices)
        {
            ent.Comp.Devices.Remove(invalidDevice);
        }

        _uiSystem.SetUiState(ent.Owner, NetworkConfiguratorUiKey.List, new NetworkConfiguratorUserInterfaceState(devices));
    }

    /// <summary>
    /// Clears the active device list when the ui is closed
    /// </summary>
    private void OnUiClosed(Entity<NetworkConfiguratorComponent> ent, ref BoundUIClosedEvent args)
    {
        if (!args.UiKey.Equals(NetworkConfiguratorUiKey.Configure)
            && !args.UiKey.Equals(NetworkConfiguratorUiKey.Link)
            && !args.UiKey.Equals(NetworkConfiguratorUiKey.List))
        {
            return;
        }

        if (TryComp(ent.Comp.ActiveDeviceList, out DeviceListComponent? list))
        {
            list.Configurators.Remove(ent);
        }

        ent.Comp.ActiveDeviceList = null;

        if (args.UiKey is NetworkConfiguratorUiKey.Link)
        {
            ent.Comp.ActiveDeviceLink = null;
            ent.Comp.DeviceLinkTarget = null;
        }
    }

    public void OnDeviceListShutdown(Entity<NetworkConfiguratorComponent?> conf, Entity<DeviceListComponent> list)
    {
        list.Comp.Configurators.Remove(conf.Owner);
        if (Resolve(conf.Owner, ref conf.Comp))
            conf.Comp.ActiveDeviceList = null;
    }

    /// <summary>
    /// Removes a device from the saved devices list
    /// </summary>
    private void OnRemoveDevice(Entity<NetworkConfiguratorComponent> ent, ref NetworkConfiguratorRemoveDeviceMessage args)
    {
        if (ent.Comp.Devices.TryGetValue(args.Address, out var removedDevice))
        {
            _adminLogger.Add(LogType.DeviceLinking,
                LogImpact.Low,
                $"{ToPrettyString(args.Actor):actor} removed buffered device {ToPrettyString(removedDevice):subject} from {ToPrettyString(ent):tool}");
        }

        ent.Comp.Devices.Remove(args.Address);
        if (TryComp(removedDevice, out DeviceNetworkComponent? device))
            device.Configurators.Remove(ent);

        UpdateListUiState(ent);
    }

    /// <summary>
    /// Clears the saved devices
    /// </summary>
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
            if (_deviceNetworkQuery.TryGetComponent(device, out var comp))
                comp.Configurators.Remove(ent);
        }

        ent.Comp.Devices.Clear();
    }

    private void OnClearLinks(Entity<NetworkConfiguratorComponent> ent, ref NetworkConfiguratorClearLinksMessage args)
    {
        if (!ent.Comp.ActiveDeviceLink.HasValue || !ent.Comp.DeviceLinkTarget.HasValue)
            return;

        _adminLogger.Add(LogType.DeviceLinking,
            LogImpact.Low,
            $"{ToPrettyString(args.Actor):actor} cleared links between {ToPrettyString(ent.Comp.ActiveDeviceLink.Value):subject} and {ToPrettyString(ent.Comp.DeviceLinkTarget.Value):subject2} with {ToPrettyString(ent):tool}");

        if (HasComp<DeviceLinkSourceComponent>(ent.Comp.ActiveDeviceLink) && HasComp<DeviceLinkSinkComponent>(ent.Comp.DeviceLinkTarget))
        {
            _deviceLinkSystem.RemoveSinkFromSource(
                ent.Comp.ActiveDeviceLink.Value,
                ent.Comp.DeviceLinkTarget.Value
                );

            UpdateLinkUiState(
                ent,
                ent.Comp.ActiveDeviceLink.Value,
                ent.Comp.DeviceLinkTarget.Value
                );
        }
        else if (HasComp<DeviceLinkSourceComponent>(ent.Comp.DeviceLinkTarget) && HasComp<DeviceLinkSinkComponent>(ent.Comp.ActiveDeviceLink))
        {
            _deviceLinkSystem.RemoveSinkFromSource(
                ent.Comp.DeviceLinkTarget.Value,
                ent.Comp.ActiveDeviceLink.Value
                );

            UpdateLinkUiState(
                ent,
                ent.Comp.DeviceLinkTarget.Value,
                ent.Comp.ActiveDeviceLink.Value
                );
        }
    }

    private void OnToggleLinks(Entity<NetworkConfiguratorComponent> ent, ref NetworkConfiguratorToggleLinkMessage args)
    {
        if (!ent.Comp.ActiveDeviceLink.HasValue || !ent.Comp.DeviceLinkTarget.HasValue)
            return;

        if (TryComp(ent.Comp.ActiveDeviceLink, out DeviceLinkSourceComponent? activeSource) && TryComp(ent.Comp.DeviceLinkTarget, out DeviceLinkSinkComponent? targetSink))
        {
            _deviceLinkSystem.ToggleLink(
                args.Actor,
                (ent.Comp.ActiveDeviceLink.Value, activeSource),
                (ent.Comp.DeviceLinkTarget.Value, targetSink),
                args.Source,
                args.Sink);

            UpdateLinkUiState(ent, (ent.Comp.ActiveDeviceLink.Value, activeSource), ent.Comp.DeviceLinkTarget.Value);
        }
        else if (TryComp(ent.Comp.DeviceLinkTarget, out DeviceLinkSourceComponent? targetSource) && TryComp(ent.Comp.ActiveDeviceLink, out DeviceLinkSinkComponent? activeSink))
        {
            _deviceLinkSystem.ToggleLink(
                args.Actor,
                (ent.Comp.DeviceLinkTarget.Value, targetSource),
                (ent.Comp.ActiveDeviceLink.Value, activeSink),
                args.Source,
                args.Sink);

            UpdateLinkUiState(
                ent,
                (ent.Comp.DeviceLinkTarget.Value, targetSource),
                ent.Comp.ActiveDeviceLink.Value);
        }
    }

    /// <summary>
    /// Saves links set by the device link UI
    /// </summary>
    private void OnSaveLinks(Entity<NetworkConfiguratorComponent> ent, ref NetworkConfiguratorLinksSaveMessage args)
    {
        if (!ent.Comp.ActiveDeviceLink.HasValue || !ent.Comp.DeviceLinkTarget.HasValue)
            return;

        if (TryComp(ent.Comp.ActiveDeviceLink, out DeviceLinkSourceComponent? activeSource) && TryComp(ent.Comp.DeviceLinkTarget, out DeviceLinkSinkComponent? targetSink))
        {
            _deviceLinkSystem.SaveLinks(
                args.Actor,
                (ent.Comp.ActiveDeviceLink.Value, activeSource),
                (ent.Comp.DeviceLinkTarget.Value, targetSink),
                args.Links);

            UpdateLinkUiState(
                ent.Owner,
                (ent.Comp.ActiveDeviceLink.Value, activeSource),
                ent.Comp.DeviceLinkTarget.Value);
        }
        else if (TryComp(ent.Comp.DeviceLinkTarget, out DeviceLinkSourceComponent? targetSource) && TryComp(ent.Comp.ActiveDeviceLink, out DeviceLinkSinkComponent? activeSink))
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
                    if (_deviceNetworkQuery.TryGetComponent(device, out var comp))
                    {
                        ent.Comp.Devices.Add(addr, device);
                        comp.Configurators.Add(ent);
                    }
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
                    .Select(v => (v.Key, MetaData(v.Value).EntityName))
                    .ToHashSet()));
    }

    public void OnDeviceShutdown(Entity<NetworkConfiguratorComponent?> conf, Entity<DeviceNetworkComponent> device)
    {
        device.Comp.Configurators.Remove(conf.Owner);
        if (!Resolve(conf.Owner, ref conf.Comp))
            return;

        foreach (var (addr, dev) in conf.Comp.Devices)
        {
            if (device.Owner == dev)
                conf.Comp.Devices.Remove(addr);
        }

        UpdateListUiState(conf!);
    }
}
