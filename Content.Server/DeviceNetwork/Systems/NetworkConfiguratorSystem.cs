using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.DeviceLinking.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Database;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Systems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Map.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.DeviceNetwork.Systems;

[UsedImplicitly]
public sealed class NetworkConfiguratorSystem : SharedNetworkConfiguratorSystem
{
    [Dependency] private readonly DeviceListSystem _deviceListSystem = default!;
    [Dependency] private readonly DeviceLinkSystem _deviceLinkSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly AccessReaderSystem _accessSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NetworkConfiguratorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<NetworkConfiguratorComponent, ComponentShutdown>(OnShutdown);

        //Interaction
        SubscribeLocalEvent<NetworkConfiguratorComponent, AfterInteractEvent>(AfterInteract); //TODO: Replace with utility verb?
        SubscribeLocalEvent<NetworkConfiguratorComponent, ExaminedEvent>(DoExamine);

        //Verbs
        SubscribeLocalEvent<NetworkConfiguratorComponent, GetVerbsEvent<UtilityVerb>>(OnAddInteractVerb);
        SubscribeLocalEvent<DeviceNetworkComponent, GetVerbsEvent<AlternativeVerb>>(OnAddAlternativeSaveDeviceVerb);
        SubscribeLocalEvent<NetworkConfiguratorComponent, GetVerbsEvent<AlternativeVerb>>(OnAddSwitchModeVerb);

        //UI
        SubscribeLocalEvent<NetworkConfiguratorComponent, BoundUIClosedEvent>(OnUiClosed);
        SubscribeLocalEvent<NetworkConfiguratorComponent, NetworkConfiguratorRemoveDeviceMessage>(OnRemoveDevice);
        SubscribeLocalEvent<NetworkConfiguratorComponent, NetworkConfiguratorClearDevicesMessage>(OnClearDevice);
        SubscribeLocalEvent<NetworkConfiguratorComponent, NetworkConfiguratorLinksSaveMessage>(OnSaveLinks);
        SubscribeLocalEvent<NetworkConfiguratorComponent, NetworkConfiguratorClearLinksMessage>(OnClearLinks);
        SubscribeLocalEvent<NetworkConfiguratorComponent, NetworkConfiguratorToggleLinkMessage>(OnToggleLinks);
        SubscribeLocalEvent<NetworkConfiguratorComponent, NetworkConfiguratorButtonPressedMessage>(OnConfigButtonPressed);

        SubscribeLocalEvent<NetworkConfiguratorComponent, BoundUserInterfaceCheckRangeEvent>(OnUiRangeCheck);

        SubscribeLocalEvent<DeviceListComponent, ComponentRemove>(OnComponentRemoved);

        SubscribeLocalEvent<BeforeSerializationEvent>(OnMapSave);
    }

    private void OnMapSave(BeforeSerializationEvent ev)
    {
        var enumerator = AllEntityQuery<NetworkConfiguratorComponent>();
        while (enumerator.MoveNext(out var uid, out var conf))
        {
            if (!TryComp(conf.ActiveDeviceList, out TransformComponent? listXform))
                continue;

            if (!ev.MapIds.Contains(listXform.MapID))
                continue;

            // The linked device list is (probably) being saved. Make sure that the configurator is also being saved
            // (i.e., not in the hands of a mapper/ghost). In the future, map saving should raise a separate event
            // containing a set of all entities that are about to be saved, which would make checking this much easier.
            // This is a shitty bandaid, and will force close the UI during auto-saves.
            // TODO Map serialization refactor
            // I'm refactoring it now and I still dont know what to do

            var xform = Transform(uid);
            if (ev.MapIds.Contains(xform.MapID) && IsSaveable(uid))
                continue;

            _uiSystem.CloseUi(uid, NetworkConfiguratorUiKey.Configure);
            DebugTools.AssertNull(conf.ActiveDeviceList);
        }

        bool IsSaveable(EntityUid uid)
        {
            while (uid.IsValid())
            {
                if (Prototype(uid)?.MapSavable == false)
                    return false;
                uid = Transform(uid).ParentUid;
            }
            return true;
        }
    }

    private void OnUiRangeCheck(Entity<NetworkConfiguratorComponent> ent, ref BoundUserInterfaceCheckRangeEvent args)
    {
        if (ent.Comp.ActiveDeviceList == null || args.Result == BoundUserInterfaceRangeResult.Fail)
            return;

        DebugTools.Assert(Exists(ent.Comp.ActiveDeviceList));
        if (!_interactionSystem.InRangeUnobstructed(args.Actor!, ent.Comp.ActiveDeviceList.Value))
            args.Result = BoundUserInterfaceRangeResult.Fail;
    }

    private void OnShutdown(EntityUid uid, NetworkConfiguratorComponent component, ComponentShutdown args)
    {
        ClearDevices(uid, component);

        if (TryComp(component.ActiveDeviceList, out DeviceListComponent? list))
            list.Configurators.Remove(uid);
        component.ActiveDeviceList = null;
    }

    private void OnMapInit(EntityUid uid, NetworkConfiguratorComponent component, MapInitEvent args)
    {
        UpdateListUiState(uid, component);
    }

    private void TryAddNetworkDevice(EntityUid? targetUid, EntityUid configuratorUid, EntityUid userUid,
        NetworkConfiguratorComponent? configurator = null)
    {
        if (!Resolve(configuratorUid, ref configurator))
            return;

        TryAddNetworkDevice(configuratorUid, targetUid, userUid, configurator);
    }

    private void TryAddNetworkDevice(EntityUid configuratorUid, EntityUid? targetUid, EntityUid userUid, NetworkConfiguratorComponent configurator, DeviceNetworkComponent? device = null)
    {
        if (!targetUid.HasValue || !Resolve(targetUid.Value, ref device, false))
            return;

        //This checks if the device is marked as having a savable address,
        //to avoid adding pdas and whatnot to air alarms. This flag is true
        //by default, so this will only prevent devices from being added to
        //network configurator lists if manually set to false in the prototype
        if (!device.SavableAddress)
            return;

        var address = device.Address;
        if (string.IsNullOrEmpty(address))
        {
            // This primarily checks if the entity in question is pre-map init or not.
            // This is because otherwise, anything that uses DeviceNetwork will not
            // have an address populated, as all devices that use DeviceNetwork
            // obtain their address on map init. If the entity is post-map init,
            // and it still doesn't have an address, it will fail. Otherwise,
            // it stores the entity's UID as a string for visual effect, that way
            // a mapper can reference the devices they've gathered by UID, instead of
            // by device network address. These entries, if the multitool is still in
            // the map after it being saved, are cleared upon mapinit.
            if (MetaData(targetUid.Value).EntityLifeStage == EntityLifeStage.MapInitialized)
            {
                _popupSystem.PopupCursor(Loc.GetString("network-configurator-device-failed", ("device", targetUid)),
                    userUid);
                return;
            }

            address = $"UID: {targetUid.Value}";
        }

        if (configurator.Devices.ContainsValue(targetUid.Value))
        {
            _popupSystem.PopupCursor(Loc.GetString("network-configurator-device-already-saved", ("device", targetUid)), userUid);
            return;
        }

        device.Configurators.Add(configuratorUid);
        configurator.Devices.Add(address, targetUid.Value);
        _popupSystem.PopupCursor(Loc.GetString("network-configurator-device-saved", ("address", device.Address), ("device", targetUid)),
            userUid, PopupType.Medium);

        _adminLogger.Add(LogType.DeviceLinking, LogImpact.Low, $"{ToPrettyString(userUid):actor} saved {ToPrettyString(targetUid.Value):subject} to {ToPrettyString(configuratorUid):tool}");

        UpdateListUiState(configuratorUid, configurator);
    }

    private void TryLinkDevice(EntityUid uid, NetworkConfiguratorComponent configurator, EntityUid? target, EntityUid user)
    {
        if (!HasComp<DeviceLinkSourceComponent>(target) && !HasComp<DeviceLinkSinkComponent>(target))
            return;

        if (configurator.ActiveDeviceLink == target)
        {
            _popupSystem.PopupEntity(Loc.GetString("network-configurator-link-mode-stopped"), target.Value, user);
            configurator.ActiveDeviceLink = null;
            return;
        }

        if (configurator.ActiveDeviceLink.HasValue
            && (HasComp<DeviceLinkSourceComponent>(target)
            && HasComp<DeviceLinkSinkComponent>(configurator.ActiveDeviceLink)
            || HasComp<DeviceLinkSinkComponent>(target)
            && HasComp<DeviceLinkSourceComponent>(configurator.ActiveDeviceLink)))
        {
            OpenDeviceLinkUi(uid, target, user, configurator);
            return;
        }

        if (HasComp<DeviceLinkSourceComponent>(target) && HasComp<DeviceLinkSourceComponent>(configurator.ActiveDeviceLink)
            || HasComp<DeviceLinkSinkComponent>(target) && HasComp<DeviceLinkSinkComponent>(configurator.ActiveDeviceLink))
            return;

        _popupSystem.PopupEntity(Loc.GetString("network-configurator-link-mode-started", ("device", Name(target.Value))), target.Value, user);
        configurator.ActiveDeviceLink = target;
    }

    private void TryLinkDefaults(EntityUid _, NetworkConfiguratorComponent configurator, EntityUid? targetUid, EntityUid user)
    {
        if (!configurator.LinkModeActive || !configurator.ActiveDeviceLink.HasValue
            || !targetUid.HasValue || configurator.ActiveDeviceLink == targetUid)
            return;

        if (!HasComp<DeviceLinkSourceComponent>(targetUid) && !HasComp<DeviceLinkSinkComponent>(targetUid))
            return;

        if (TryComp(configurator.ActiveDeviceLink, out DeviceLinkSourceComponent? activeSource) && TryComp(targetUid, out DeviceLinkSinkComponent? targetSink))
        {
            _deviceLinkSystem.LinkDefaults(user, configurator.ActiveDeviceLink.Value, targetUid.Value, activeSource, targetSink);
        }
        else if (TryComp(configurator.ActiveDeviceLink, out DeviceLinkSinkComponent? activeSink) && TryComp(targetUid, out DeviceLinkSourceComponent? targetSource))
        {
            _deviceLinkSystem.LinkDefaults(user, targetUid.Value, configurator.ActiveDeviceLink.Value, targetSource, activeSink);
        }
    }

    private bool AccessCheck(EntityUid target, EntityUid? user, NetworkConfiguratorComponent component)
    {
        if (!TryComp(target, out AccessReaderComponent? reader) || user == null)
            return true;

        if (_accessSystem.IsAllowed(user.Value, target, reader))
            return true;

        _audioSystem.PlayPvs(component.SoundNoAccess, user.Value, AudioParams.Default.WithVolume(-2f).WithPitchScale(1.2f));
        _popupSystem.PopupEntity(Loc.GetString("network-configurator-device-access-denied"), target, user.Value);

        return false;
    }

    private void OnComponentRemoved(EntityUid uid, DeviceListComponent component, ComponentRemove args)
    {
        _uiSystem.CloseUi(uid, NetworkConfiguratorUiKey.Configure);
    }

    /// <summary>
    /// Toggles between linking and listing mode
    /// </summary>
    private void SwitchMode(EntityUid? userUid, EntityUid configuratorUid, NetworkConfiguratorComponent configurator)
    {
        if (Delay(configurator))
            return;

        configurator.LinkModeActive = !configurator.LinkModeActive;

        if (!userUid.HasValue)
            return;

        if (!configurator.LinkModeActive)
            configurator.ActiveDeviceLink = null;

        UpdateModeAppearance(userUid.Value, configuratorUid, configurator);
    }

    /// <summary>
    /// Sets the mode to linking or list depending on the link mode parameter
    /// </summary>>
    private void SetMode(EntityUid configuratorUid, NetworkConfiguratorComponent configurator, EntityUid userUid, bool linkMode)
    {
        configurator.LinkModeActive = linkMode;

        if (!linkMode)
            configurator.ActiveDeviceLink = null;

        UpdateModeAppearance(userUid, configuratorUid, configurator);
    }

    /// <summary>
    /// Updates the configurators appearance and plays a sound indicating that the mode switched
    /// </summary>
    private void UpdateModeAppearance(EntityUid userUid, EntityUid configuratorUid, NetworkConfiguratorComponent configurator)
    {
        Dirty(configuratorUid, configurator);
        _appearanceSystem.SetData(configuratorUid, NetworkConfiguratorVisuals.Mode, configurator.LinkModeActive);

        var pitch = configurator.LinkModeActive ? 1 : 0.8f;
        _audioSystem.PlayPvs(configurator.SoundSwitchMode, userUid, AudioParams.Default.WithVolume(1.5f).WithPitchScale(pitch));
    }

    /// <summary>
    /// Returns true if the last time this method was called is earlier than the configurators use delay.
    /// </summary>
    private bool Delay(NetworkConfiguratorComponent configurator)
    {
        var currentTime = _gameTiming.CurTime;
        if (currentTime < configurator.LastUseAttempt + configurator.UseDelay)
            return true;

        configurator.LastUseAttempt = currentTime;
        return false;
    }

    #region Interactions

    private void DoExamine(EntityUid uid, NetworkConfiguratorComponent component, ExaminedEvent args)
    {
        var mode = component.LinkModeActive ? "network-configurator-examine-mode-link" : "network-configurator-examine-mode-list";
        args.PushMarkup(Loc.GetString("network-configurator-examine-current-mode", ("mode", Loc.GetString(mode))));
    }

    private void AfterInteract(EntityUid uid, NetworkConfiguratorComponent component, AfterInteractEvent args)
    {
        OnUsed(uid, component, args.Target, args.User, args.CanReach);
    }

    /// <summary>
    /// Either adds a device to the device list or shows the config ui if the target is ant entity with a device list
    /// </summary>
    private void OnUsed(EntityUid uid, NetworkConfiguratorComponent configurator, EntityUid? target, EntityUid user, bool canReach = true)
    {
        if (!canReach || !target.HasValue)
            return;

        DetermineMode(uid, configurator, target, user);

        if (configurator.LinkModeActive)
        {
            TryLinkDevice(uid, configurator, target, user);
            return;
        }

        if (!HasComp<DeviceListComponent>(target))
        {
            TryAddNetworkDevice(uid, target, user, configurator);
            return;
        }

        OpenDeviceListUi(uid, target, user, configurator);
    }

    private void DetermineMode(EntityUid configuratorUid, NetworkConfiguratorComponent configurator, EntityUid? target, EntityUid userUid)
    {
        var hasLinking = HasComp<DeviceLinkSinkComponent>(target) || HasComp<DeviceLinkSourceComponent>(target);

        if (hasLinking && HasComp<DeviceListComponent>(target) || hasLinking == configurator.LinkModeActive)
            return;

        var hasNetworking = HasComp<DeviceNetworkComponent>(target);
        if (hasNetworking)
            SetMode(configuratorUid, configurator, userUid, false);
        else if (hasLinking)
            SetMode(configuratorUid, configurator, userUid, true);
    }

    #endregion

    #region Verbs

    /// <summary>
    /// Adds the interaction verb which is either configuring device lists or saving a device onto the configurator
    /// </summary>
    private void OnAddInteractVerb(EntityUid uid, NetworkConfiguratorComponent configurator, GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.Using.HasValue)
            return;

        var verb = new UtilityVerb
        {
            Act = () => OnUsed(uid, configurator, args.Target, args.User),
            Impact = LogImpact.Low
        };

        if (configurator.LinkModeActive && (HasComp<DeviceLinkSinkComponent>(args.Target) || HasComp<DeviceLinkSourceComponent>(args.Target)))
        {
            var linkStarted = configurator.ActiveDeviceLink.HasValue;
            verb.Text = Loc.GetString(linkStarted ? "network-configurator-link" : "network-configurator-start-link");
            verb.Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/in.svg.192dpi.png"));
            args.Verbs.Add(verb);
        }
        else if (HasComp<DeviceNetworkComponent>(args.Target))
        {
            var isDeviceList = HasComp<DeviceListComponent>(args.Target);
            verb.Text = Loc.GetString(isDeviceList ? "network-configurator-configure" : "network-configurator-save-device");
            verb.Icon = isDeviceList
                ? new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/settings.svg.192dpi.png"))
                : new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/in.svg.192dpi.png"));
            args.Verbs.Add(verb);
        }
    }

    /// <summary>
    /// Powerful. Funny alt interact using.
    /// Adds an alternative verb for saving a device on the configurator for entities with the <see cref="DeviceListComponent"/>.
    /// Allows alt clicking entities with a network configurator that would otherwise trigger a different action like entities
    /// with a <see cref="DeviceListComponent"/>
    /// </summary>
    private void OnAddAlternativeSaveDeviceVerb(EntityUid uid, DeviceNetworkComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.Using.HasValue
            || !TryComp<NetworkConfiguratorComponent>(args.Using.Value, out var configurator))
            return;

        if (!configurator.LinkModeActive && HasComp<DeviceListComponent>(args.Target))
        {
            AlternativeVerb verb = new()
            {
                Text = Loc.GetString("network-configurator-save-device"),
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/in.svg.192dpi.png")),
                Act = () => TryAddNetworkDevice(args.Target, args.Using.Value, args.User),
                Impact = LogImpact.Low
            };
            args.Verbs.Add(verb);
            return;
        }

        if (configurator is { LinkModeActive: true, ActiveDeviceLink: { } }
        && (HasComp<DeviceLinkSinkComponent>(args.Target) || HasComp<DeviceLinkSourceComponent>(args.Target)))
        {
            AlternativeVerb verb = new()
            {
                Text = Loc.GetString("network-configurator-link-defaults"),
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/in.svg.192dpi.png")),
                Act = () => TryLinkDefaults(args.Using.Value, configurator, args.Target, args.User),
                Impact = LogImpact.Low
            };
            args.Verbs.Add(verb);
        }
    }

    private void OnAddSwitchModeVerb(EntityUid uid, NetworkConfiguratorComponent configurator, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.Using.HasValue || !HasComp<NetworkConfiguratorComponent>(args.Target))
            return;

        AlternativeVerb verb = new()
        {
            Text = Loc.GetString("network-configurator-switch-mode"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")),
            Act = () => SwitchMode(args.User, args.Target, configurator),
            Impact = LogImpact.Low
        };
        args.Verbs.Add(verb);
    }

    #endregion

    #region UI

    private void OpenDeviceLinkUi(EntityUid configuratorUid, EntityUid? targetUid, EntityUid userUid, NetworkConfiguratorComponent configurator)
    {
        if (Delay(configurator))
            return;

        if (!targetUid.HasValue || !configurator.ActiveDeviceLink.HasValue || !AccessCheck(targetUid.Value, userUid, configurator))
            return;


        _uiSystem.OpenUi(configuratorUid, NetworkConfiguratorUiKey.Link, userUid);
        configurator.DeviceLinkTarget = targetUid;


        if (TryComp(configurator.ActiveDeviceLink, out DeviceLinkSourceComponent? activeSource) && TryComp(targetUid, out DeviceLinkSinkComponent? targetSink))
        {
            UpdateLinkUiState(configuratorUid, configurator.ActiveDeviceLink.Value, targetUid.Value, activeSource, targetSink);
        }
        else if (TryComp(configurator.ActiveDeviceLink, out DeviceLinkSinkComponent? activeSink)
                 && TryComp(targetUid, out DeviceLinkSourceComponent? targetSource))
        {
            UpdateLinkUiState(configuratorUid, targetUid.Value, configurator.ActiveDeviceLink.Value, targetSource, activeSink);
        }
    }

    private void UpdateLinkUiState(EntityUid configuratorUid, EntityUid sourceUid, EntityUid sinkUid,
        DeviceLinkSourceComponent? sourceComponent = null, DeviceLinkSinkComponent? sinkComponent = null,
        DeviceNetworkComponent? sourceNetworkComponent = null, DeviceNetworkComponent? sinkNetworkComponent = null)
    {
        if (!Resolve(sourceUid, ref sourceComponent, false) || !Resolve(sinkUid, ref sinkComponent, false))
            return;

        var sources = _deviceLinkSystem.GetSourcePorts(sourceUid, sourceComponent);
        var sinks = _deviceLinkSystem.GetSinkPortIds((sinkUid, sinkComponent));
        var links = _deviceLinkSystem.GetLinks(sourceUid, sinkUid, sourceComponent);
        var defaults = _deviceLinkSystem.GetDefaults(sources);
        var sourceIds = sources.Select(s => (ProtoId<SourcePortPrototype>)s.ID).ToArray();

        var sourceAddress = Resolve(sourceUid, ref sourceNetworkComponent, false) ? sourceNetworkComponent.Address : "";
        var sinkAddress = Resolve(sinkUid, ref sinkNetworkComponent, false) ? sinkNetworkComponent.Address : "";

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
            _uiSystem.SetUiState(configuratorUid, NetworkConfiguratorUiKey.Configure, new DeviceListUserInterfaceState(
                _deviceListSystem.GetDeviceList(configurator.ActiveDeviceList.Value)
                    .Select(v => (v.Key, MetaData(v.Value).EntityName)).ToHashSet()
            ));
        }
    }

    /// <summary>
    /// Sends the list of saved devices to the ui
    /// </summary>
    private void UpdateListUiState(EntityUid uid, NetworkConfiguratorComponent component)
    {
        HashSet<(string address, string name)> devices = new();
        HashSet<string> invalidDevices = new();

        foreach (var pair in component.Devices)
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
            component.Devices.Remove(invalidDevice);
        }

        _uiSystem.SetUiState(uid, NetworkConfiguratorUiKey.List, new NetworkConfiguratorUserInterfaceState(devices));
    }

    /// <summary>
    /// Clears the active device list when the ui is closed
    /// </summary>
    private void OnUiClosed(EntityUid uid, NetworkConfiguratorComponent component, BoundUIClosedEvent args)
    {
        if (!args.UiKey.Equals(NetworkConfiguratorUiKey.Configure)
            && !args.UiKey.Equals(NetworkConfiguratorUiKey.Link)
            && !args.UiKey.Equals(NetworkConfiguratorUiKey.List))
        {
            return;
        }

        if (TryComp(component.ActiveDeviceList, out DeviceListComponent? list))
        {
            list.Configurators.Remove(uid);
        }

        component.ActiveDeviceList = null;

        if (args.UiKey is NetworkConfiguratorUiKey.Link)
        {
            component.ActiveDeviceLink = null;
            component.DeviceLinkTarget = null;
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
    private void OnRemoveDevice(EntityUid uid, NetworkConfiguratorComponent component, NetworkConfiguratorRemoveDeviceMessage args)
    {
        if (component.Devices.TryGetValue(args.Address, out var removedDevice))
        {
            _adminLogger.Add(LogType.DeviceLinking, LogImpact.Low,
                $"{ToPrettyString(args.Actor):actor} removed buffered device {ToPrettyString(removedDevice):subject} from {ToPrettyString(uid):tool}");
        }

        component.Devices.Remove(args.Address);
        if (TryComp(removedDevice, out DeviceNetworkComponent? device))
            device.Configurators.Remove(uid);

        UpdateListUiState(uid, component);
    }

    /// <summary>
    /// Clears the saved devices
    /// </summary>
    private void OnClearDevice(EntityUid uid, NetworkConfiguratorComponent component, NetworkConfiguratorClearDevicesMessage args)
    {
        _adminLogger.Add(LogType.DeviceLinking, LogImpact.Low,
            $"{ToPrettyString(args.Actor):actor} cleared buffered devices from {ToPrettyString(uid):tool}");

        ClearDevices(uid, component);
        UpdateListUiState(uid, component);
    }

    private void ClearDevices(EntityUid uid, NetworkConfiguratorComponent component)
    {
        var query = GetEntityQuery<DeviceNetworkComponent>();
        foreach (var device in component.Devices.Values)
        {
            if (query.TryGetComponent(device, out var comp))
                comp.Configurators.Remove(uid);
        }

        component.Devices.Clear();
    }

    private void OnClearLinks(EntityUid uid, NetworkConfiguratorComponent configurator, NetworkConfiguratorClearLinksMessage args)
    {
        if (!configurator.ActiveDeviceLink.HasValue || !configurator.DeviceLinkTarget.HasValue)
            return;

        _adminLogger.Add(LogType.DeviceLinking, LogImpact.Low,
            $"{ToPrettyString(args.Actor):actor} cleared links between {ToPrettyString(configurator.ActiveDeviceLink.Value):subject} and {ToPrettyString(configurator.DeviceLinkTarget.Value):subject2} with {ToPrettyString(uid):tool}");

        if (HasComp<DeviceLinkSourceComponent>(configurator.ActiveDeviceLink) && HasComp<DeviceLinkSinkComponent>(configurator.DeviceLinkTarget))
        {
            _deviceLinkSystem.RemoveSinkFromSource(
                configurator.ActiveDeviceLink.Value,
                configurator.DeviceLinkTarget.Value
                );

            UpdateLinkUiState(
                uid,
                configurator.ActiveDeviceLink.Value,
                configurator.DeviceLinkTarget.Value
                );
        }
        else if (HasComp<DeviceLinkSourceComponent>(configurator.DeviceLinkTarget) && HasComp<DeviceLinkSinkComponent>(configurator.ActiveDeviceLink))
        {
            _deviceLinkSystem.RemoveSinkFromSource(
                configurator.DeviceLinkTarget.Value,
                configurator.ActiveDeviceLink.Value
                );

            UpdateLinkUiState(
                uid,
                configurator.DeviceLinkTarget.Value,
                configurator.ActiveDeviceLink.Value
                );
        }
    }

    private void OnToggleLinks(EntityUid uid, NetworkConfiguratorComponent configurator, NetworkConfiguratorToggleLinkMessage args)
    {
        if (!configurator.ActiveDeviceLink.HasValue || !configurator.DeviceLinkTarget.HasValue)
            return;

        if (TryComp(configurator.ActiveDeviceLink, out DeviceLinkSourceComponent? activeSource) && TryComp(configurator.DeviceLinkTarget, out DeviceLinkSinkComponent? targetSink))
        {
            _deviceLinkSystem.ToggleLink(
                args.Actor,
                configurator.ActiveDeviceLink.Value,
                configurator.DeviceLinkTarget.Value,
                args.Source, args.Sink,
                activeSource, targetSink);

            UpdateLinkUiState(uid, configurator.ActiveDeviceLink.Value, configurator.DeviceLinkTarget.Value, activeSource);
        }
        else if (TryComp(configurator.DeviceLinkTarget, out DeviceLinkSourceComponent? targetSource) && TryComp(configurator.ActiveDeviceLink, out DeviceLinkSinkComponent? activeSink))
        {
            _deviceLinkSystem.ToggleLink(
                args.Actor,
                configurator.DeviceLinkTarget.Value,
                configurator.ActiveDeviceLink.Value,
                args.Source, args.Sink,
                targetSource, activeSink
                );

            UpdateLinkUiState(
                uid,
                configurator.DeviceLinkTarget.Value,
                configurator.ActiveDeviceLink.Value,
                targetSource
                );
        }
    }

    /// <summary>
    /// Saves links set by the device link UI
    /// </summary>
    private void OnSaveLinks(EntityUid uid, NetworkConfiguratorComponent configurator, NetworkConfiguratorLinksSaveMessage args)
    {
        if (!configurator.ActiveDeviceLink.HasValue || !configurator.DeviceLinkTarget.HasValue)
            return;

        if (TryComp(configurator.ActiveDeviceLink, out DeviceLinkSourceComponent? activeSource) && TryComp(configurator.DeviceLinkTarget, out DeviceLinkSinkComponent? targetSink))
        {
            _deviceLinkSystem.SaveLinks(
                args.Actor,
                configurator.ActiveDeviceLink.Value,
                configurator.DeviceLinkTarget.Value,
                args.Links,
                activeSource,
                targetSink
                );

            UpdateLinkUiState(
                uid,
                configurator.ActiveDeviceLink.Value,
                configurator.DeviceLinkTarget.Value,
                activeSource
                );
        }
        else if (TryComp(configurator.DeviceLinkTarget, out DeviceLinkSourceComponent? targetSource) && TryComp(configurator.ActiveDeviceLink, out DeviceLinkSinkComponent? activeSink))
        {
            _deviceLinkSystem.SaveLinks(
                args.Actor,
                configurator.DeviceLinkTarget.Value,
                configurator.ActiveDeviceLink.Value,
                args.Links,
                targetSource,
                activeSink
                );

            UpdateLinkUiState(
                uid,
                configurator.DeviceLinkTarget.Value,
                configurator.ActiveDeviceLink.Value,
                targetSource
                );
        }
    }

    /// <summary>
    /// Handles all the button presses from the config ui.
    /// Modifies, copies or visualizes the targets device list
    /// </summary>
    private void OnConfigButtonPressed(EntityUid uid, NetworkConfiguratorComponent component, NetworkConfiguratorButtonPressedMessage args)
    {
        if (!component.ActiveDeviceList.HasValue)
            return;

        var result = DeviceListUpdateResult.NoComponent;
        switch (args.ButtonKey)
        {
            case NetworkConfiguratorButtonKey.Set:
                _adminLogger.Add(LogType.DeviceLinking, LogImpact.Low,
                    $"{ToPrettyString(args.Actor):actor} set device links to {ToPrettyString(component.ActiveDeviceList.Value):subject} with {ToPrettyString(uid):tool}");

                result = _deviceListSystem.UpdateDeviceList(component.ActiveDeviceList.Value, new HashSet<EntityUid>(component.Devices.Values));
                break;
            case NetworkConfiguratorButtonKey.Add:
                _adminLogger.Add(LogType.DeviceLinking, LogImpact.Low,
                    $"{ToPrettyString(args.Actor):actor} added device links to {ToPrettyString(component.ActiveDeviceList.Value):subject} with {ToPrettyString(uid):tool}");

                result = _deviceListSystem.UpdateDeviceList(component.ActiveDeviceList.Value, new HashSet<EntityUid>(component.Devices.Values), true);
                break;
            case NetworkConfiguratorButtonKey.Clear:
                _adminLogger.Add(LogType.DeviceLinking, LogImpact.Low,
                    $"{ToPrettyString(args.Actor):actor} cleared device links from {ToPrettyString(component.ActiveDeviceList.Value):subject} with {ToPrettyString(uid):tool}");
                result = _deviceListSystem.UpdateDeviceList(component.ActiveDeviceList.Value, new HashSet<EntityUid>());
                break;
            case NetworkConfiguratorButtonKey.Copy:
                _adminLogger.Add(LogType.DeviceLinking, LogImpact.Low,
                    $"{ToPrettyString(args.Actor):actor} copied devices from {ToPrettyString(component.ActiveDeviceList.Value):subject} to {ToPrettyString(uid):tool}");

                ClearDevices(uid, component);

                var query = GetEntityQuery<DeviceNetworkComponent>();
                foreach (var (addr, device) in _deviceListSystem.GetDeviceList(component.ActiveDeviceList.Value))
                {
                    if (query.TryGetComponent(device, out var comp))
                    {
                        component.Devices.Add(addr, device);
                        comp.Configurators.Add(uid);
                    }
                }
                UpdateListUiState(uid, component);
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
            uid,
            NetworkConfiguratorUiKey.Configure,
            new DeviceListUserInterfaceState(
                _deviceListSystem.GetDeviceList(component.ActiveDeviceList.Value)
                    .Select(v => (v.Key, MetaData(v.Value).EntityName)).ToHashSet()));
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

        UpdateListUiState(conf, conf.Comp);
    }

    private void OnUiOpenAttempt(EntityUid uid, NetworkConfiguratorComponent configurator, ActivatableUIOpenAttemptEvent args)
    {
        if (configurator.LinkModeActive)
            args.Cancel();
    }
    #endregion
}
