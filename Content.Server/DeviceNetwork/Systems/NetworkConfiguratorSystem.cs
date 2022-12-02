using System.Linq;
using Content.Server.DeviceNetwork.Components;
using Content.Server.UserInterface;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Database;
using Content.Shared.DeviceNetwork;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.DeviceNetwork.Systems;

[UsedImplicitly]
public sealed class NetworkConfiguratorSystem : SharedNetworkConfiguratorSystem
{
    [Dependency] private readonly DeviceListSystem _deviceListSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly AccessReaderSystem _accessSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NetworkConfiguratorComponent, MapInitEvent>(OnMapInit);

        //Interaction
        SubscribeLocalEvent<NetworkConfiguratorComponent, AfterInteractEvent>((uid, component, args) => OnUsed(uid, component, args.Target, args.User, args.CanReach)); //TODO: Replace with utility verb?

        //Verbs
        SubscribeLocalEvent<NetworkConfiguratorComponent, GetVerbsEvent<UtilityVerb>>(OnAddInteractVerb);
        SubscribeLocalEvent<DeviceNetworkComponent, GetVerbsEvent<AlternativeVerb>>(OnAddAlternativeSaveDeviceVerb);

        //UI
        SubscribeLocalEvent<NetworkConfiguratorComponent, BoundUIClosedEvent>(OnUiClosed);
        SubscribeLocalEvent<NetworkConfiguratorComponent, NetworkConfiguratorRemoveDeviceMessage>(OnRemoveDevice);
        SubscribeLocalEvent<NetworkConfiguratorComponent, NetworkConfiguratorClearDevicesMessage>(OnClearDevice);
        SubscribeLocalEvent<NetworkConfiguratorComponent, NetworkConfiguratorButtonPressedMessage>(OnConfigButtonPressed);

        SubscribeLocalEvent<DeviceListComponent, ComponentRemove>(OnComponentRemoved);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var component in EntityManager.EntityQuery<NetworkConfiguratorComponent>())
        {
            if (component.ActiveDeviceList != null && EntityManager.EntityExists(component.ActiveDeviceList.Value) &&
                _interactionSystem.InRangeUnobstructed(component.Owner, component.ActiveDeviceList.Value))
            {
                return;
            }

            //The network configurator is a handheld device. There can only ever be an ui session open for the player holding the device.
            _uiSystem.GetUiOrNull(component.Owner, NetworkConfiguratorUiKey.Configure)?.CloseAll();
        }
    }

    private void OnMapInit(EntityUid uid, NetworkConfiguratorComponent component, MapInitEvent args)
    {
        component.Devices.Clear();
        UpdateUiState(uid, component);
    }

    private void TryAddNetworkDevice(EntityUid? targetUid, EntityUid configuratorUid, EntityUid userUid,
        NetworkConfiguratorComponent? configurator = null)
    {
        if (!Resolve(configuratorUid, ref configurator))
            return;

        TryAddNetworkDevice(targetUid, userUid, configurator);
    }

    private void TryAddNetworkDevice(EntityUid? targetUid, EntityUid userUid, NetworkConfiguratorComponent configurator, DeviceNetworkComponent? device = null)
    {
        if (!targetUid.HasValue || !Resolve(targetUid.Value, ref device, false))
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

            address = $"UID: {targetUid.Value.ToString()}";
        }

        if (configurator.Devices.ContainsValue(targetUid.Value))
        {
            _popupSystem.PopupCursor(Loc.GetString("network-configurator-device-already-saved", ("device", targetUid)), userUid);
            return;
        }

        configurator.Devices.Add(address, targetUid.Value);
        _popupSystem.PopupCursor(Loc.GetString("network-configurator-device-saved", ("address", device.Address), ("device", targetUid)),
            userUid, PopupType.Medium);

        UpdateUiState(configurator.Owner, configurator);
    }

    private bool AccessCheck(EntityUid target, EntityUid? user, NetworkConfiguratorComponent component)
    {
        if (!TryComp(target, out AccessReaderComponent? reader) || user == null)
            return false;

        if (_accessSystem.IsAllowed(user.Value, reader))
            return true;

        SoundSystem.Play(component.SoundNoAccess.GetSound(), Filter.Pvs(user.Value), target, AudioParams.Default.WithVolume(-2f).WithPitchScale(1.2f));
        _popupSystem.PopupEntity(Loc.GetString("network-configurator-device-access-denied"), target, user.Value);

        return false;
    }

    private void OnComponentRemoved(EntityUid uid, DeviceListComponent component, ComponentRemove args)
    {
        _uiSystem.GetUiOrNull(component.Owner, NetworkConfiguratorUiKey.Configure)?.CloseAll();
    }

    #region Interactions

    /// <summary>
    /// Either adds a device to the device list or shows the config ui if the target is ant entity with a device list
    /// </summary>
    private void OnUsed(EntityUid uid, NetworkConfiguratorComponent component, EntityUid? target, EntityUid user, bool canReach = true)
    {
        if (!canReach)
            return;

        if (!HasComp<DeviceListComponent>(target))
        {
            TryAddNetworkDevice(target, user, component);
            return;
        }

        OpenDeviceListUi(target, user, component);
    }

    #endregion

    #region Verbs

    /// <summary>
    /// Adds the interaction verb which is either configuring device lists or saving a device onto the configurator
    /// </summary>
    private void OnAddInteractVerb(EntityUid uid, NetworkConfiguratorComponent component, GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.Using.HasValue || !HasComp<DeviceNetworkComponent>(args.Target))
            return;

        var isDeviceList = HasComp<DeviceListComponent>(args.Target);

        UtilityVerb verb = new()
        {
            Text = Loc.GetString(isDeviceList ? "network-configurator-configure" : "network-configurator-save-device"),
            IconTexture = isDeviceList ? "/Textures/Interface/VerbIcons/settings.svg.192dpi.png" : "/Textures/Interface/VerbIcons/in.svg.192dpi.png",
            Act = () => OnUsed(uid, component, args.Target, args.User),
            Impact = LogImpact.Low
        };
        args.Verbs.Add(verb);
    }

    /// <summary>
    /// Powerful. Funny alt interact using.
    /// Adds an alternative verb for saving a device on the configurator for entities with the <see cref="DeviceListComponent"/>.
    /// Allows alt clicking entities with a network configurator that would otherwise trigger a different action like entities
    /// with a <see cref="DeviceListComponent"/>
    /// </summary>
    private void OnAddAlternativeSaveDeviceVerb(EntityUid uid, DeviceNetworkComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.Using.HasValue || !HasComp<NetworkConfiguratorComponent>(args.Using.Value)
            || !HasComp<DeviceListComponent>(args.Target))
            return;

        AlternativeVerb verb = new()
        {
            Text = Loc.GetString("network-configurator-save-device"),
            IconTexture = "/Textures/Interface/VerbIcons/in.svg.192dpi.png",
            Act = () => TryAddNetworkDevice(args.Target, args.Using.Value, args.User),
            Impact = LogImpact.Low
        };
        args.Verbs.Add(verb);
    }

    #endregion

    #region UI

    /// <summary>
    /// Opens the config ui. It can be used to modify the devices in the targets device list.
    /// </summary>
    private void OpenDeviceListUi(EntityUid? targetUid, EntityUid userUid, NetworkConfiguratorComponent configurator)
    {
        if (!targetUid.HasValue || !TryComp(userUid, out ActorComponent? actor) || !AccessCheck(targetUid.Value, userUid, configurator))
            return;

        configurator.ActiveDeviceList = targetUid;
        Dirty(configurator);
        _uiSystem.GetUiOrNull(configurator.Owner, NetworkConfiguratorUiKey.Configure)?.Open(actor.PlayerSession);
        _uiSystem.TrySetUiState(
            configurator.Owner,
            NetworkConfiguratorUiKey.Configure,
            new DeviceListUserInterfaceState(
                _deviceListSystem.GetDeviceList(configurator.ActiveDeviceList.Value)
                    .Select(v => (v.Key, MetaData(v.Value).EntityName)).ToHashSet()));
    }

    /// <summary>
    /// Sends the list of saved devices to the ui
    /// </summary>
    private void UpdateUiState(EntityUid uid, NetworkConfiguratorComponent component)
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

        _uiSystem.GetUiOrNull(uid, NetworkConfiguratorUiKey.List)?.SetState(new NetworkConfiguratorUserInterfaceState(devices));
    }

    /// <summary>
    /// Clears the active device list when the ui is closed
    /// </summary>
    private void OnUiClosed(EntityUid uid, NetworkConfiguratorComponent component, BoundUIClosedEvent args)
    {
        component.ActiveDeviceList = null;
    }

    /// <summary>
    /// Removes a device from the saved devices list
    /// </summary>
    private void OnRemoveDevice(EntityUid uid, NetworkConfiguratorComponent component, NetworkConfiguratorRemoveDeviceMessage args)
    {
        component.Devices.Remove(args.Address);
        UpdateUiState(uid, component);
    }

    /// <summary>
    /// Clears the saved devices
    /// </summary>
    private void OnClearDevice(EntityUid uid, NetworkConfiguratorComponent component, NetworkConfiguratorClearDevicesMessage _)
    {
        component.Devices.Clear();
        UpdateUiState(uid, component);
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
                result = _deviceListSystem.UpdateDeviceList(component.ActiveDeviceList.Value, new HashSet<EntityUid>(component.Devices.Values));
                break;
            case NetworkConfiguratorButtonKey.Add:
                result = _deviceListSystem.UpdateDeviceList(component.ActiveDeviceList.Value, new HashSet<EntityUid>(component.Devices.Values), true);
                break;
            case NetworkConfiguratorButtonKey.Clear:
                result = _deviceListSystem.UpdateDeviceList(component.ActiveDeviceList.Value, new HashSet<EntityUid>());
                break;
            case NetworkConfiguratorButtonKey.Copy:
                component.Devices = _deviceListSystem.GetDeviceList(component.ActiveDeviceList.Value);
                UpdateUiState(uid, component);
                return;
            case NetworkConfiguratorButtonKey.Show:
                // This should be done client-side.
                // _deviceListSystem.ToggleVisualization(component.ActiveDeviceList.Value);
                break;
        }

        var resultText = result switch
        {
            DeviceListUpdateResult.TooManyDevices => Loc.GetString("network-configurator-too-many-devices"),
            DeviceListUpdateResult.UpdateOk => Loc.GetString("network-configurator-update-ok"),
            _ => "error"
        };

        _popupSystem.PopupCursor(Loc.GetString(resultText), args.Session, PopupType.Medium);
        _uiSystem.TrySetUiState(
            component.Owner,
            NetworkConfiguratorUiKey.Configure,
            new DeviceListUserInterfaceState(
                _deviceListSystem.GetDeviceList(component.ActiveDeviceList.Value)
                    .Select(v => (v.Key, MetaData(v.Value).EntityName)).ToHashSet()));
    }
    #endregion
}
