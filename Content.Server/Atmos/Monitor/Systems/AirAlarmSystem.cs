using System;
using System.Collections.Generic;
using Content.Server.Access.Components;
using Content.Server.Access.Systems;
using Content.Server.Atmos.Monitor.Components;
using Content.Server.Atmos.Piping.Components;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.UserInterface;
using Content.Server.WireHacking;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Monitor.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;

namespace Content.Server.Atmos.Monitor.Systems
{
    // AirAlarm system - specific for atmos devices, rather than
    // atmos monitors.
    //
    // oh boy, message passing!
    //
    // Commands should always be sent into packet's Command
    // data key. In response, a packet will be transmitted
    // with the response type as its command, and the
    // response data in its data key.
    public class AirAlarmSystem : EntitySystem
    {
        [Dependency] private readonly DeviceNetworkSystem _deviceNet = default!;
        [Dependency] private readonly AtmosMonitorSystem _atmosMonitorSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly AccessReaderSystem _accessSystem = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;

        public const int Freq = AtmosMonitorSystem.AtmosMonitorApcFreq;

        // -- Atmos Device Commands --

        // Toggles the device on or off.
        //
        // Disabled. Sync/set device data should do this already.
        // public const string AirAlarmToggleCmd = "air_alarm_toggle_device";

        // Gets the type of atmos device.
        //
        // Disabled, since sync should technically get this already.
        //
        // public const string AirAlarmGetType = "air_alarm_get_type";

        // Sets the data on the atmos device. This can be any object,
        // and as long as the type matches to the data, it will
        // succeed. This is both a command and data key.
        public const string AirAlarmSetData = "air_alarm_set_device_data";

        // Request a sync from the devices in the network.
        public const string AirAlarmSyncCmd = "air_alarm_sync_devices";

        // Sets the air alarm's program.
        public const string AirAlarmSetMode = "air_alarm_set_mode";

        // -- Packet Data --

        // ToggleData. This should just echo what the device's
        // enabled/disabled state is.
        //
        // Disabled. This should also be in set_device_data.
        // public const string AirAlarmToggleData = "air_alarm_toggle_device_data";

        // TypeData. This should hold the type of device that it is,
        // so that the UI knows what kind of widget to draw, or
        // to ensure that the data being sent is considered
        // correct.
        //
        // Disabled. This should be in the sync data packet.
        // public const string AirAlarmTypeData = "air_alarm_type_data";

        // SetDataStatus. This should be a bool
        // that returns true if the op succeeded,
        // or false if it failed.
        public const string AirAlarmSetDataStatus = "air_alarm_set_device_data_status";

        // Sync data. This could be a number of things, depending
        // on the atmos devices in the network. A successful sync
        // will have this as the packet command, with data
        // keyed by this string containing all the sync data required.
        //
        // (fyi this might be literal component data passed through)
        public const string AirAlarmSyncData = "air_alarm_device_sync_data";

        // -- API --

        public void SetData(EntityUid uid, string address, IAtmosDeviceData data)
        {
            if (EntityManager.TryGetComponent(uid, out AtmosMonitorComponent monitor)
                && !monitor.NetEnabled)
                return;

            var payload = new NetworkPayload
            {
                [DeviceNetworkConstants.Command] = AirAlarmSetData,
                // [AirAlarmTypeData] = type,
                [AirAlarmSetData] = data
            };

            _deviceNet.QueuePacket(uid, address, Freq, payload);
        }

        public void SyncAllDevices(EntityUid uid)
        {
            if (EntityManager.TryGetComponent(uid, out AtmosMonitorComponent monitor)
                && !monitor.NetEnabled)
                return;

            var payload = new NetworkPayload
            {
                [DeviceNetworkConstants.Command] = AirAlarmSyncCmd
            };

            _deviceNet.QueuePacket(uid, string.Empty, Freq, payload, true);
        }

        public void SyncDevice(EntityUid uid, string address)
        {
            if (EntityManager.TryGetComponent(uid, out AtmosMonitorComponent monitor)
                && !monitor.NetEnabled)
                return;


            var payload = new NetworkPayload
            {
                [DeviceNetworkConstants.Command] = AirAlarmSyncCmd
            };

            _deviceNet.QueuePacket(uid, address, Freq, payload);
        }

        public void SyncMode(EntityUid uid, AirAlarmMode mode)
        {
            if (EntityManager.TryGetComponent(uid, out AtmosMonitorComponent monitor)
                && !monitor.NetEnabled)
                return;

            var payload = new NetworkPayload
            {
                [DeviceNetworkConstants.Command] = AirAlarmSetMode,
                [AirAlarmSetMode] = mode
            };

            _deviceNet.QueuePacket(uid, string.Empty, Freq, payload, true);
        }

        // -- Internal --

        public override void Initialize()
        {
            SubscribeLocalEvent<AirAlarmComponent, PacketSentEvent>(OnPacketRecv);
            SubscribeLocalEvent<AirAlarmComponent, AtmosDeviceUpdateEvent>(OnAtmosUpdate);
            SubscribeLocalEvent<AirAlarmComponent, AtmosMonitorAlarmEvent>(OnAtmosAlarm);
            SubscribeLocalEvent<AirAlarmComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<AirAlarmComponent, AirAlarmResyncAllDevicesMessage>(OnResyncAll);
            SubscribeLocalEvent<AirAlarmComponent, AirAlarmUpdateAlarmModeMessage>(OnUpdateAlarmMode);
            SubscribeLocalEvent<AirAlarmComponent, AirAlarmUpdateAlarmThresholdMessage>(OnUpdateThreshold);
            SubscribeLocalEvent<AirAlarmComponent, AirAlarmUpdateDeviceDataMessage>(OnUpdateDeviceData);
            SubscribeLocalEvent<AirAlarmComponent, BoundUIClosedEvent>(OnClose);
            SubscribeLocalEvent<AirAlarmComponent, InteractHandEvent>(OnInteract);
        }

        private void OnPowerChanged(EntityUid uid, AirAlarmComponent component, PowerChangedEvent args)
        {
            if (!args.Powered)
            {
                ForceCloseAllInterfaces(uid);
                component.CurrentModeUpdater = null;
                component.DeviceData.Clear();
            }
        }

        private void OnClose(EntityUid uid, AirAlarmComponent component, BoundUIClosedEvent args)
        {
            component.ActivePlayers.Remove(args.Session.UserId);
            if (component.ActivePlayers.Count == 0)
                RemoveActiveInterface(uid);
        }

        private void OnInteract(EntityUid uid, AirAlarmComponent component, InteractHandEvent args)
        {
            if (!_interactionSystem.InRangeUnobstructed(args.User, args.Target))
                return;

            if (!args.User.TryGetComponent(out ActorComponent? actor))
                return;

            if (EntityManager.TryGetComponent(uid, out WiresComponent wire))
                if (wire.IsPanelOpen)
                {
                    args.Handled = false;
                    return;
                }

            if (EntityManager.TryGetComponent(uid, out ApcPowerReceiverComponent recv))
                if (!recv.Powered)
                    return;

            component.Owner.GetUIOrNull(SharedAirAlarmInterfaceKey.Key)?.Open(actor.PlayerSession);
            component.ActivePlayers.Add(actor.PlayerSession.UserId);
            AddActiveInterface(uid);
            SendAddress(uid);
            SendAlarmMode(uid);
            SendThresholds(uid);
            SyncAllDevices(uid);
            SendAirData(uid);
        }

        private void OnResyncAll(EntityUid uid, AirAlarmComponent component, AirAlarmResyncAllDevicesMessage args)
        {
            if (AccessCheck(uid, args.Session.AttachedEntity, component))
            {
                component.DeviceData.Clear();
                SyncAllDevices(uid);
            }
        }

        private void OnUpdateAlarmMode(EntityUid uid, AirAlarmComponent component, AirAlarmUpdateAlarmModeMessage args)
        {
            string addr = string.Empty;
            if (EntityManager.TryGetComponent(uid, out DeviceNetworkComponent netConn)) addr = netConn.Address;
            if (AccessCheck(uid, args.Session.AttachedEntity, component))
                SetMode(uid, addr, args.Mode, true, false);
            else
                SendAlarmMode(uid);
        }

        private void OnUpdateThreshold(EntityUid uid, AirAlarmComponent component, AirAlarmUpdateAlarmThresholdMessage args)
        {
            if (AccessCheck(uid, args.Session.AttachedEntity, component))
                SetThreshold(uid, args.Threshold, args.Type, args.Gas);
            else
                SendThresholds(uid);
        }

        private void OnUpdateDeviceData(EntityUid uid, AirAlarmComponent component, AirAlarmUpdateDeviceDataMessage args)
        {
            if (AccessCheck(uid, args.Session.AttachedEntity, component))
                SetDeviceData(uid, args.Address, args.Data);
            else
                SyncDevice(uid, args.Address);
        }

        private bool AccessCheck(EntityUid uid, IEntity? user, AirAlarmComponent? component = null)
        {
            if (Resolve(uid, ref component))
                if (EntityManager.TryGetComponent(uid, out AccessReader reader))
                    if (user != null)
                        if (_accessSystem.IsAllowed(reader, user.Uid) || component.FullAccess)
                            return true;
                        else
                            user.PopupMessage(Loc.GetString("air-alarm-ui-access-denied"));

            return false;
        }

        private void OnAtmosAlarm(EntityUid uid, AirAlarmComponent component, AtmosMonitorAlarmEvent args)
        {
            if (component.ActivePlayers.Count != 0)
            {
                SyncAllDevices(uid);
                SendAirData(uid);
            }

            string addr = string.Empty;
            if (EntityManager.TryGetComponent(uid, out DeviceNetworkComponent netConn)) addr = netConn.Address;


            if (args.HighestNetworkType == AtmosMonitorAlarmType.Danger)
            {
                SetMode(uid, addr, AirAlarmMode.None, true);
                // set mode to off to mimic the vents/scrubbers being turned off
                // update UI
                //
                // no, the mode isn't processed here - it's literally just
                // set to what mimics 'off'
            }
            else if (args.HighestNetworkType == AtmosMonitorAlarmType.Normal)
            {
                // if the mode is still set to off, set it to filtering instead
                // alternatively, set it to the last saved mode
                //
                // no, this still doesn't execute the mode
                SetMode(uid, addr, AirAlarmMode.Filtering, true);
            }
        }

        public void SetThreshold(EntityUid uid, AtmosAlarmThreshold threshold, AtmosMonitorThresholdType type, Gas? gas = null, AirAlarmComponent? controller = null)
        {
            if (!Resolve(uid, ref controller)) return;

            _atmosMonitorSystem.SetThreshold(uid, type, threshold, gas);

            _uiSystem.TrySendUiMessage(uid, SharedAirAlarmInterfaceKey.Key, new AirAlarmUpdateAlarmThresholdMessage(type, threshold, gas));
        }

        public void SetMode(EntityUid uid, string origin, AirAlarmMode mode, bool sync = false, bool uiOnly = true, AirAlarmComponent? controller = null)
        {
            if (!Resolve(uid, ref controller)) return;
            controller.CurrentMode = mode;

            // setting it to UI only maans we don't have
            // to deal with the issue of not-single-owner
            // alarm mode executors
            if (!uiOnly)
            {
                var newMode = AirAlarmModeFactory.ModeToExecutor(mode);
                if (newMode != null)
                {
                    newMode.Execute(uid);
                    if (newMode is IAirAlarmModeUpdate updatedMode)
                    {
                        controller.CurrentModeUpdater = updatedMode;
                        controller.CurrentModeUpdater.NetOwner = origin;
                    }
                    else if (controller.CurrentModeUpdater != null)
                        controller.CurrentModeUpdater = null;
                }
            }
            // only one air alarm in a network can use an air alarm mode
            // that updates, so even if it's a ui-only change,
            // we have to invalidte the last mode's updater and
            // remove it because otherwise it'll execute a now
            // invalid mode
            else if (controller.CurrentModeUpdater != null
                     && controller.CurrentModeUpdater.NetOwner != origin)
                controller.CurrentModeUpdater = null;

            // controller.SendMessage(new AirAlarmUpdateAlarmModeMessage(mode));
            _uiSystem.TrySendUiMessage(uid, SharedAirAlarmInterfaceKey.Key, new AirAlarmUpdateAlarmModeMessage(mode));


            // setting sync deals with the issue of air alarms
            // in the same network needing to have the same mode
            // as other alarms
            if (sync) SyncMode(uid, mode);
        }

        public void SetDeviceData(EntityUid uid, string address, IAtmosDeviceData devData, AirAlarmComponent? controller = null)
        {
            if (!Resolve(uid, ref controller)) return;

            devData.Dirty = true;
            SetData(uid, address, devData);
        }

        private void OnPacketRecv(EntityUid uid, AirAlarmComponent controller, PacketSentEvent args)
        {
            Logger.DebugS("AirAlarmSystem", $"Received packet from {args.SenderAddress}");
            if (!args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? cmd))
                return;

            switch (cmd)
            {
                case AirAlarmSyncData:
                    if (!args.Data.TryGetValue(AirAlarmSyncData, out IAtmosDeviceData? data)
                        || data == null
                        || !controller.CanSync) break;

                    // Save into component.
                    // Sync data to interface.
                    // _airAlarmDataSystem.UpdateDeviceData(uid, args.SenderAddress, data);
                    //
                    _uiSystem.TrySendUiMessage(uid, SharedAirAlarmInterfaceKey.Key, new AirAlarmUpdateDeviceDataMessage(args.SenderAddress, data));
                    if (controller.WiresComponent != null) controller.UpdateWires();
                    if (!controller.DeviceData.TryAdd(args.SenderAddress, data))
                        controller.DeviceData[args.SenderAddress] = data;

                    return;
                case AirAlarmSetDataStatus:
                    if (!args.Data.TryGetValue(AirAlarmSetDataStatus, out bool dataStatus)) break;

                    // Sync data to interface.
                    // This should say if the result
                    // failed, or succeeded. Don't save it.l
                    SyncDevice(uid, args.SenderAddress);

                    return;
                case AirAlarmSetMode:
                    if (!args.Data.TryGetValue(AirAlarmSetMode, out AirAlarmMode alarmMode)) break;

                    SetMode(uid, args.SenderAddress, alarmMode);

                    return;
            }
        }

        // <-- UI stuff -->

        // List of active user interfaces.
        private HashSet<EntityUid> _activeUserInterfaces = new();

        // Add an active interface for updating.
        public void AddActiveInterface(EntityUid uid)
        {
            _activeUserInterfaces.Add(uid);
        }

        // Remove an active interface from updating.
        public void RemoveActiveInterface(EntityUid uid)
        {
            _activeUserInterfaces.Remove(uid);
        }

        public void ForceCloseAllInterfaces(EntityUid uid)
        {
            _uiSystem.TryCloseAll(uid, SharedAirAlarmInterfaceKey.Key);
        }

        private void SendAddress(EntityUid uid, DeviceNetworkComponent? netConn = null)
        {
            if (!Resolve(uid, ref netConn)) return;

            _uiSystem.TrySendUiMessage(uid, SharedAirAlarmInterfaceKey.Key, new AirAlarmSetAddressMessage(netConn.Address));
        }

        // Update an interface's air data. This is all the 'hot' data
        // that an air alarm contains server-side. Updated with a whopping 16
        // delay automatically once a UI is in the loop.
        public void SendAirData(EntityUid uid, AirAlarmComponent? alarm = null, AtmosMonitorComponent? monitor = null, ApcPowerReceiverComponent? power = null)
        {
            if (!Resolve(uid, ref alarm, ref monitor, ref power)) return;

            if (!power.Powered) return;


            if (monitor.TileGas != null)
            {
                var gases = new Dictionary<Gas, float>();

                foreach (var gas in Enum.GetValues<Gas>())
                    gases.Add(gas, monitor.TileGas.GetMoles(gas));

                var airData = new AirAlarmAirData(monitor.TileGas.Pressure, monitor.TileGas.Temperature, monitor.TileGas.TotalMoles, monitor.LastAlarmState, gases);

                Logger.DebugS("AirAlarmSystem", "Attempting to update data now.");

                _uiSystem.TrySendUiMessage(uid, SharedAirAlarmInterfaceKey.Key, new AirAlarmUpdateAirDataMessage(airData));
            }
        }

        public void SendAlarmMode(EntityUid uid, AtmosMonitorComponent? monitor = null, ApcPowerReceiverComponent? power = null, AirAlarmComponent? controller = null)
        {
            if (!Resolve(uid, ref monitor, ref power, ref controller)
                || !power.Powered) return;

            _uiSystem.TrySendUiMessage(uid, SharedAirAlarmInterfaceKey.Key, new AirAlarmUpdateAlarmModeMessage(controller.CurrentMode));
        }

        public void SendThresholds(EntityUid uid, AtmosMonitorComponent? monitor = null, ApcPowerReceiverComponent? power = null, AirAlarmComponent? controller = null)
        {
            if (!Resolve(uid, ref monitor, ref power, ref controller)
                || !power.Powered) return;

            if (monitor.PressureThreshold == null
                && monitor.TemperatureThreshold == null
                && monitor.GasThresholds == null)
                return;

            if (monitor.PressureThreshold != null)
            {
                _uiSystem.TrySendUiMessage(uid, SharedAirAlarmInterfaceKey.Key, new AirAlarmUpdateAlarmThresholdMessage(AtmosMonitorThresholdType.Pressure, monitor.PressureThreshold));
            }

            if (monitor.TemperatureThreshold != null)
            {
                _uiSystem.TrySendUiMessage(uid, SharedAirAlarmInterfaceKey.Key, new AirAlarmUpdateAlarmThresholdMessage(AtmosMonitorThresholdType.Temperature, monitor.TemperatureThreshold));
            }

            if (monitor.GasThresholds != null)
            {
                foreach (var (gas, threshold) in monitor.GasThresholds)
                    _uiSystem.TrySendUiMessage(uid, SharedAirAlarmInterfaceKey.Key, new AirAlarmUpdateAlarmThresholdMessage(AtmosMonitorThresholdType.Gas, threshold, gas));
            }
        }

        public void OnAtmosUpdate(EntityUid uid, AirAlarmComponent alarm, AtmosDeviceUpdateEvent args)
        {
            if (alarm.CurrentModeUpdater != null)
                alarm.CurrentModeUpdater.Update(uid);
        }

        private const float _delay = 8f;
        private float _timer = 0f;

        public override void Update(float frameTime)
        {
            _timer += frameTime;
            if (_timer >= _delay)
            {
                _timer = 0f;
                foreach (var uid in _activeUserInterfaces)
                {
                    SendAirData(uid);
                    _uiSystem.TrySetUiState(uid, SharedAirAlarmInterfaceKey.Key, new AirAlarmUIState());
                }
            }
        }
    }
}
