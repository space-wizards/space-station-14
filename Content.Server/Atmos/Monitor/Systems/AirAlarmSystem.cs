using System;
using System.Collections.Generic;
using Content.Server.Atmos.Monitor.Components;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Power.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Monitor.Components;
using Content.Shared.Atmos.Monitor.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

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


        public const int Freq = AtmosMonitorSystem.AtmosMonitorApcFreq;

        // -- Atmos Device Commands --

        // Toggles the device on or off.
        public const string AirAlarmToggleCmd = "air_alarm_toggle_device";

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
        public const string AirAlarmToggleData = "air_alarm_toggle_device_data";

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

        public void ToggleDevice(EntityUid uid, string address, bool toggle)
        {
            var payload = new NetworkPayload
            {
                [DeviceNetworkConstants.Command] = AirAlarmToggleCmd,
                [AirAlarmToggleCmd] = toggle
            };

            _deviceNet.QueuePacket(uid, address, Freq, payload);
        }

        // -- Internal --

        public override void Initialize()
        {
            SubscribeLocalEvent<AirAlarmComponent, ComponentStartup>(OnComponentStartup);
            SubscribeLocalEvent<AirAlarmComponent, PacketSentEvent>(OnPacketRecv);
            SubscribeLocalEvent<AtmosMonitorComponent, AirAlarmSetThresholdEvent>(OnSetThreshold);
            SubscribeLocalEvent<AirAlarmDataComponent, AirAlarmSetModeEvent>(OnSetMode);
            SubscribeLocalEvent<AirAlarmDataComponent, AirAlarmDeviceDataUpdateEvent>(OnDeviceDataUpdate);
        }

        private void OnComponentStartup(EntityUid uid, AirAlarmComponent component, ComponentStartup args)
        {
            SyncAllDevices(uid);
        }

        private void OnSetThreshold(EntityUid uid, AtmosMonitorComponent data, AirAlarmSetThresholdEvent args)
        {
            switch (args.Type)
            {
                case AtmosMonitorThresholdType.Pressure:
                    data.PressureThreshold = args.Threshold;
                    break;
                case AtmosMonitorThresholdType.Temperature:
                    data.TemperatureThreshold = args.Threshold;
                    break;
                case AtmosMonitorThresholdType.Gas:
                    if (args.Gas == null || data.GasThresholds == null) return;
                    data.GasThresholds[(Gas) args.Gas] = args.Threshold;
                    break;
            }
        }

        private void OnSetMode(EntityUid uid, AirAlarmDataComponent data, AirAlarmSetModeEvent args)
        {
            // TODO: Mode setting/programs.
        }

        private void OnDeviceDataUpdate(EntityUid uid, AirAlarmDataComponent data, AirAlarmDeviceDataUpdateEvent args)
        {
            SetData(uid, args.Address, data.DeviceData[args.Address]);
        }

        private void OnPacketRecv(EntityUid uid, AirAlarmComponent controller, PacketSentEvent args)
        {
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
                    if (!alarmData.DeviceData.TryAdd(args.SenderAddress, data))
                        alarmData.DeviceData[args.SenderAddress] = data;

                    alarmData.Dirty();

                    controller.UpdateUI();

                    return;
                case AirAlarmToggleData:
                    if (!args.Data.TryGetValue(AirAlarmToggleData, out bool toggleStatus)
                        || !alarmData.DeviceData.TryGetValue(args.SenderAddress, out IAtmosDeviceData? devData)) break;

                    // Save into component.
                    // Sync data to interface.
                    devData.Enabled = toggleStatus;
                    // controller.DeviceData[args.SenderAddress] = devData; out vars are refs?

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

        // Update an interface's data. This is all the 'hot' data
        // that an air alarm contains server-side. Updated with a whopping 16
        // delay automatically once a UI is in the loop.
        public void UpdateInterfaceData(EntityUid uid, AirAlarmComponent? alarm = null, AirAlarmDataComponent? data = null, AtmosMonitorComponent? monitor = null, ApcPowerReceiverComponent? power = null)
        {
            if (!Resolve(uid, ref alarm, ref data, ref monitor, ref power)) return;

            if (!power.Powered) return;

            if (monitor.TileGas != null)
            {
                data.Pressure = monitor.TileGas.Pressure;
                data.Temperature = monitor.TileGas.Pressure;
                data.TotalMoles = monitor.TileGas.TotalMoles;

                foreach (var gas in Enum.GetValues<Gas>())
                    if (!data.Gases.TryAdd(gas, monitor.TileGas.GetMoles(gas)))
                        data.Gases[gas] = monitor.TileGas.GetMoles(gas);
            }

            data.Dirty();
            alarm.UpdateUI();
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
                    UpdateInterfaceData(uid);
            }
        }
    }
}
