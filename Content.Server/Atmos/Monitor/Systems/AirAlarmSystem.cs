using System;
using System.Collections.Generic;
using Content.Server.Atmos.Monitor.Components;
using Content.Server.Atmos.Piping.Components;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.WireHacking;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Monitor.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;

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
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;

        #region Device Network API

        public const int Freq = AtmosMonitorSystem.AtmosMonitorApcFreq;

        /// <summary>
        ///     Command to set device data within the air alarm's network.
        /// </summary>
        public const string AirAlarmSetData = "air_alarm_set_device_data";

        /// <summary>
        ///     Command to request a sync from devices in an air alarm's network.
        /// </summary>
        public const string AirAlarmSyncCmd = "air_alarm_sync_devices";

        /// <summary>
        ///     Command to set an air alarm's mode.
        /// </summary>
        public const string AirAlarmSetMode = "air_alarm_set_mode";

        // -- Packet Data --

        /// <summary>
        ///     Data response to an AirAlarmSetData command.
        /// </summary>
        public const string AirAlarmSetDataStatus = "air_alarm_set_device_data_status";

        /// <summary>
        ///     Data response to an AirAlarmSync command. Contains
        ///     IAtmosDeviceData in this system's implementation.
        /// </summary>
        public const string AirAlarmSyncData = "air_alarm_device_sync_data";

        // -- API --

        /// <summary>
        ///     Set the data for an air alarm managed device.
        /// </summary>
        /// <param name="address">The address of the device.</param>
        /// <param name="data">The data to send to the device.</param>
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

        /// <summary>
        ///     Broadcast a sync packet to an air alarm's local network.
        /// </summary>
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

        /// <summary>
        ///     Send a sync packet to a specific device from an air alarm.
        /// </summary>
        /// <param name="address">The address of the device.</param>
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

        /// <summary>
        ///     Sync this air alarm's mode with the rest of the network.
        /// </summary>
        /// <param name="mode">The mode to sync with the rest of the network.</param>
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

        #endregion

        #region Events

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

            if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
                return;

            if (EntityManager.TryGetComponent(uid, out WiresComponent wire) && wire.IsPanelOpen)
            {
                args.Handled = false;
                return;
            }

            if (EntityManager.TryGetComponent(uid, out ApcPowerReceiverComponent recv) && !recv.Powered)
                return;

            _uiSystem.GetUiOrNull(component.Owner, SharedAirAlarmInterfaceKey.Key)?.Open(actor.PlayerSession);
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

        private bool AccessCheck(EntityUid uid, EntityUid? user, AirAlarmComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return false;

            if (!EntityManager.TryGetComponent(uid, out AccessReaderComponent reader) || user == null)
                return false;

            if (!_accessSystem.IsAllowed(reader, user.Value) && !component.FullAccess)
            {
                _popup.PopupEntity(Loc.GetString("air-alarm-ui-access-denied"), user.Value, Filter.Entities(user.Value));
                return false;
            }

            return true;
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

        #endregion

        #region Air Alarm Settings

        /// <summary>
        ///     Set a threshold on an air alarm.
        /// </summary>
        /// <param name="threshold">New threshold data.</param>
        public void SetThreshold(EntityUid uid, AtmosAlarmThreshold threshold, AtmosMonitorThresholdType type, Gas? gas = null, AirAlarmComponent? controller = null)
        {
            if (!Resolve(uid, ref controller)) return;

            _atmosMonitorSystem.SetThreshold(uid, type, threshold, gas);

            _uiSystem.TrySendUiMessage(uid, SharedAirAlarmInterfaceKey.Key, new AirAlarmUpdateAlarmThresholdMessage(type, threshold, gas));
        }

        /// <summary>
        ///     Set an air alarm's mode.
        /// </summary>
        /// <param name="origin">The origin address of this mode set. Used for network sync.</param>
        /// <param name="mode">The mode to set the alarm to.</param>
        /// <param name="sync">Whether to sync this mode change to the network or not. Defaults to false.</param>
        /// <param name="uiOnly">Whether this change is for the UI only, or if it changes the air alarm's operating mode. Defaults to true.</param>
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

        /// <summary>
        ///     Sets device data. Practically a wrapper around the packet sending function, SetData.
        /// </summary>
        /// <param name="address">The address to send the new data to.</param>
        /// <param name="devData">The device data to be sent.</param>
        public void SetDeviceData(EntityUid uid, string address, IAtmosDeviceData devData, AirAlarmComponent? controller = null)
        {
            if (!Resolve(uid, ref controller)) return;

            devData.Dirty = true;
            SetData(uid, address, devData);
        }

        private void OnPacketRecv(EntityUid uid, AirAlarmComponent controller, PacketSentEvent args)
        {
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
                    if (HasComp<WiresComponent>(uid)) controller.UpdateWires();
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

        #endregion

        #region UI

        // List of active user interfaces.
        private HashSet<EntityUid> _activeUserInterfaces = new();

        /// <summary>
        ///     Adds an active interface to be updated.
        /// </summary>
        public void AddActiveInterface(EntityUid uid) =>
            _activeUserInterfaces.Add(uid);

        /// <summary>
        ///     Removes an active interface from the system update loop.
        /// </summary>
        public void RemoveActiveInterface(EntityUid uid) =>
            _activeUserInterfaces.Remove(uid);

        /// <summary>
        ///     Force closes all interfaces currently open related to this air alarm.
        /// </summary>
        public void ForceCloseAllInterfaces(EntityUid uid) =>
            _uiSystem.TryCloseAll(uid, SharedAirAlarmInterfaceKey.Key);

        private void SendAddress(EntityUid uid, DeviceNetworkComponent? netConn = null)
        {
            if (!Resolve(uid, ref netConn)) return;

            _uiSystem.TrySendUiMessage(uid, SharedAirAlarmInterfaceKey.Key, new AirAlarmSetAddressMessage(netConn.Address));
        }

        /// <summary>
        ///     Update an interface's air data. This is all the 'hot' data
        ///     that an air alarm contains server-side. Updated with a whopping 8
        ///     delay automatically once a UI is in the loop.
        /// </summary>
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

                _uiSystem.TrySendUiMessage(uid, SharedAirAlarmInterfaceKey.Key, new AirAlarmUpdateAirDataMessage(airData));
            }
        }

        /// <summary>
        ///     Send an air alarm mode to any open interface related to an air alarm.
        /// </summary>
        public void SendAlarmMode(EntityUid uid, AtmosMonitorComponent? monitor = null, ApcPowerReceiverComponent? power = null, AirAlarmComponent? controller = null)
        {
            if (!Resolve(uid, ref monitor, ref power, ref controller)
                || !power.Powered) return;

            _uiSystem.TrySendUiMessage(uid, SharedAirAlarmInterfaceKey.Key, new AirAlarmUpdateAlarmModeMessage(controller.CurrentMode));
        }

        /// <summary>
        ///     Send all thresholds to any open interface related to a given air alarm.
        /// </summary>
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

        #endregion
    }
}
