using System;
using System.Collections.Generic;
using Content.Server.Atmos.Monitor.Components;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Power.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Monitor.Components;
using Content.Shared.Atmos.Monitor.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
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
        [Dependency] private readonly AirAlarmDataSystem _airAlarmDataSystem = default!;

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
            var payload = new NetworkPayload
            {
                [DeviceNetworkConstants.Command] = AirAlarmSyncCmd
            };

            _deviceNet.QueuePacket(uid, string.Empty, Freq, payload, true);
        }

        public void SyncDevice(EntityUid uid, string address)
        {
            var payload = new NetworkPayload
            {
                [DeviceNetworkConstants.Command] = AirAlarmSyncCmd
            };

            _deviceNet.QueuePacket(uid, address, Freq, payload);
        }

        /*
        public void ToggleDevice(EntityUid uid, string address, bool toggle)
        {
            var payload = new NetworkPayload
            {
                [DeviceNetworkConstants.Command] = AirAlarmToggleCmd,
                [AirAlarmToggleCmd] = toggle
            };

            _deviceNet.QueuePacket(uid, address, Freq, payload);
        }
        */

        // -- Internal --

        public override void Initialize()
        {
            SubscribeLocalEvent<AirAlarmComponent, ComponentStartup>(OnComponentStartup);
            SubscribeLocalEvent<AirAlarmComponent, PacketSentEvent>(OnPacketRecv);
            SubscribeLocalEvent<AirAlarmDataComponent, AirAlarmSetThresholdEvent>(OnSetThreshold);
            SubscribeLocalEvent<AirAlarmDataComponent, AirAlarmSetModeEvent>(OnSetMode);
            SubscribeLocalEvent<AirAlarmDataComponent, AirAlarmDeviceDataUpdateEvent>(OnDeviceDataUpdate);
        }

        private void OnComponentStartup(EntityUid uid, AirAlarmComponent component, ComponentStartup args)
        {
        }

        private void OnSetThreshold(EntityUid uid, AirAlarmDataComponent data, AirAlarmSetThresholdEvent args)
        {
            // Justification: This data is already in the shared component. The event
            // just lets us transmit the data without having to call for the component,
            // since this is specific data.
            _atmosMonitorSystem.SetThreshold(uid, args.Type, args.Threshold, args.Gas);
        }

        private void OnSetMode(EntityUid uid, AirAlarmDataComponent data, AirAlarmSetModeEvent args)
        {
            Logger.DebugS("AirAlarmData", "Dirty air alarm mode detected.");
            Logger.DebugS("AirAlarmData", $"CurrentMode: {data.CurrentMode}");
            Logger.DebugS("AirAlarmData", $"DirtyMode: {data.DirtyMode}");
            // TODO: Mode setting/programs.
        }

        private void OnDeviceDataUpdate(EntityUid uid, AirAlarmDataComponent data, AirAlarmDeviceDataUpdateEvent args)
        {
            SetData(uid, args.Address, data.DeviceData[args.Address]);
        }

        private void OnPacketRecv(EntityUid uid, AirAlarmComponent controller, PacketSentEvent args)
        {
            Logger.DebugS("AirAlarmSystem", $"Received packet from {args.SenderAddress}");
            if (!EntityManager.TryGetComponent<AirAlarmDataComponent>(uid, out var alarmData))
                return;

            if (!args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? cmd))
                return;

            switch (cmd)
            {
                case AirAlarmSyncData:
                    if (!args.Data.TryGetValue(AirAlarmSyncData, out IAtmosDeviceData? data)
                        || data == null) break;

                    // Save into component.
                    // Sync data to interface.
                    _airAlarmDataSystem.UpdateDeviceData(uid, args.SenderAddress, data);

                    controller.UpdateUI();

                    return;
                case AirAlarmSetDataStatus:
                    if (!args.Data.TryGetValue(AirAlarmSetDataStatus, out bool dataStatus)) break;

                    // Sync data to interface.
                    // This should say if the result
                    // failed, or succeeded. Don't save it.l


                    controller.UpdateUI();

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

        // Update an interface's air data. This is all the 'hot' data
        // that an air alarm contains server-side. Updated with a whopping 16
        // delay automatically once a UI is in the loop.
        public void UpdateAirData(EntityUid uid, AirAlarmComponent? alarm = null, AtmosMonitorComponent? monitor = null, ApcPowerReceiverComponent? power = null)
        {
            if (!Resolve(uid, ref alarm, ref monitor, ref power)) return;

            if (!power.Powered) return;


            if (monitor.TileGas != null)
            {
                var gases = new Dictionary<Gas, float>();

                foreach (var gas in Enum.GetValues<Gas>())
                    gases.Add(gas, monitor.TileGas.GetMoles(gas));

                var data = new AirAlarmAirData(monitor.TileGas.Pressure, monitor.TileGas.Temperature, monitor.TileGas.TotalMoles, monitor.LastAlarmState, gases);

                Logger.DebugS("AirAlarmSystem", "Attempting to update data now.");

                _airAlarmDataSystem.UpdateAirData(uid, data);
            }

            alarm.UpdateUI();
        }

        public void SendAlarmMode(EntityUid uid, AtmosMonitorComponent? monitor = null, ApcPowerReceiverComponent? power = null, AirAlarmDataComponent? data = null)
        {
            if (!Resolve(uid, ref monitor, ref power, ref data)
                || !power.Powered) return;

            _airAlarmDataSystem.UpdateAlarmMode(uid, data.CurrentMode);
        }

        public void SendThresholds(EntityUid uid, AtmosMonitorComponent? monitor = null, ApcPowerReceiverComponent? power = null)
        {
            if (!Resolve(uid, ref monitor, ref power)
                || !power.Powered) return;

            if (monitor.PressureThreshold != null)
                _airAlarmDataSystem.UpdateAlarmThreshold(uid, monitor.PressureThreshold, AtmosMonitorThresholdType.Pressure);

            if (monitor.TemperatureThreshold != null)
                _airAlarmDataSystem.UpdateAlarmThreshold(uid, monitor.TemperatureThreshold, AtmosMonitorThresholdType.Temperature);

            if (monitor.GasThresholds != null)
                foreach (var (gas, threshold) in monitor.GasThresholds)
                    _airAlarmDataSystem.UpdateAlarmThreshold(uid, threshold, AtmosMonitorThresholdType.Gas, gas);
        }

        private const float _delay = 16f;
        private float _timer = 0f;

        public override void Update(float frameTime)
        {
            _timer += frameTime;
            if (_timer >= _delay)
            {
                _timer = 0f;
                foreach (var uid in _activeUserInterfaces)
                    UpdateAirData(uid);
            }
        }
    }
}
