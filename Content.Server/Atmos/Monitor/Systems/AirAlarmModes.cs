using System;
using System.Collections.Generic;
using Content.Server.Atmos.Monitor.Components;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Monitor.Components;
using Content.Shared.Atmos.Piping.Unary.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Atmos.Monitor
{
    /// <summary>
    ///     This is an interface that air alarm modes use
    ///     in order to execute the defined modes.
    /// </summary>
    public interface IAirAlarmMode
    {
        // This is executed the moment the mode
        // is set. This is to ensure that 'dumb'
        // modes such as Filter/Panic are immediately
        // set.
        /// <summary>
        ///     Executed the mode is set on an air alarm.
        ///     This is to ensure that modes like Filter/Panic
        ///     are immediately set.
        /// </summary>
        public void Execute(EntityUid uid);
    }

    // IAirAlarmModeUpdate
    //
    // This is an interface that AirAlarmSystem uses
    // in order to 'update' air alarm modes so that
    // modes like Replace can be implemented.
    /// <summary>
    ///     An interface that AirAlarmSystem uses
    ///     in order to update air alarm modes that
    ///     need updating (e.g., Replace)
    /// </summary>
    public interface IAirAlarmModeUpdate
    {
        /// <summary>
        ///     This is checked by AirAlarmSystem when
        ///     a mode is updated. This should be set
        ///     to a DeviceNetwork address, or some
        ///     unique identifier that ID's the
        ///     owner of the mode's executor.
        /// </summary>
        public string NetOwner { get; set; }
        /// <summary>
        ///     This is executed every time the air alarm
        ///     update loop is fully executed. This should
        ///     be where all the logic goes.
        /// </summary>
        public void Update(EntityUid uid);
    }

    public class AirAlarmModeFactory
    {
        private static IAirAlarmMode _filterMode = new AirAlarmFilterMode();
        private static IAirAlarmMode _fillMode = new AirAlarmFillMode();
        private static IAirAlarmMode _panicMode = new AirAlarmPanicMode();
        private static IAirAlarmMode _noneMode = new AirAlarmNoneMode();

        // still not a fan since ReplaceMode must have an allocation
        // but it's whatever
        public static IAirAlarmMode? ModeToExecutor(AirAlarmMode mode) => mode switch 
        {
            AirAlarmMode.Filtering => _filterMode,
            AirAlarmMode.Fill => _fillMode,
            AirAlarmMode.Panic => _panicMode,
            AirAlarmMode.None => _noneMode,
            AirAlarmMode.Replace => new AirAlarmReplaceMode(),
            _ => null
        };
    }

    // like a tiny little EntitySystem
    public abstract class AirAlarmModeExecutor : IAirAlarmMode
    {
        [Dependency] public readonly IEntityManager EntityManager = default!;
        public readonly DeviceNetworkSystem DeviceNetworkSystem;
        public readonly AirAlarmSystem AirAlarmSystem;

        public abstract void Execute(EntityUid uid);

        public AirAlarmModeExecutor()
        {
            IoCManager.InjectDependencies(this);

            DeviceNetworkSystem = EntitySystem.Get<DeviceNetworkSystem>();
            AirAlarmSystem = EntitySystem.Get<AirAlarmSystem>();
        }
    }

    public class AirAlarmNoneMode : AirAlarmModeExecutor
    {
        public override void Execute(EntityUid uid)
        {
            if (!EntityManager.TryGetComponent(uid, out AirAlarmComponent alarm))
                return;

            foreach (var (addr, device) in alarm.DeviceData)
            {
                device.Enabled = false;
                AirAlarmSystem.SetData(uid, addr, device);
            }
        }
    }

    public class AirAlarmFilterMode : AirAlarmModeExecutor
    {
        public override void Execute(EntityUid uid)
        {
            if (!EntityManager.TryGetComponent(uid, out AirAlarmComponent alarm))
                return;

            foreach (var (addr, device) in alarm.DeviceData)
            {
                switch (device)
                {
                    case GasVentPumpData pumpData:
                        AirAlarmSystem.SetData(uid, addr, GasVentPumpData.FilterModePreset);
                        break;
                    case GasVentScrubberData scrubberData:
                        AirAlarmSystem.SetData(uid, addr, GasVentScrubberData.FilterModePreset);
                        break;
                }
            }
        }
    }

    public class AirAlarmPanicMode : AirAlarmModeExecutor
    {
        public override void Execute(EntityUid uid)
        {
            if (!EntityManager.TryGetComponent(uid, out AirAlarmComponent alarm))
                return;

            foreach (var (addr, device) in alarm.DeviceData)
            {
                switch (device)
                {
                    case GasVentPumpData pumpData:
                        AirAlarmSystem.SetData(uid, addr, GasVentPumpData.PanicModePreset);
                        break;
                    case GasVentScrubberData scrubberData:
                        AirAlarmSystem.SetData(uid, addr, GasVentScrubberData.PanicModePreset);
                        break;
                }
            }
        }
    }

    public class AirAlarmFillMode : AirAlarmModeExecutor
    {
        public override void Execute(EntityUid uid)
        {
            if (!EntityManager.TryGetComponent(uid, out AirAlarmComponent alarm))
                return;

            foreach (var (addr, device) in alarm.DeviceData)
            {
                switch (device)
                {
                    case GasVentPumpData pumpData:
                        AirAlarmSystem.SetData(uid, addr, GasVentPumpData.FillModePreset);
                        break;
                    case GasVentScrubberData scrubberData:
                        AirAlarmSystem.SetData(uid, addr, GasVentScrubberData.FillModePreset);
                        break;
                }
            }
        }
    }

    public class AirAlarmReplaceMode : AirAlarmModeExecutor, IAirAlarmModeUpdate
    {
        private Dictionary<string, IAtmosDeviceData> _devices = new();
        private float _lastPressure = Atmospherics.OneAtmosphere;
        private AtmosMonitorComponent? _monitor;
        private AtmosAlarmableComponent? _alarmable;

        public string NetOwner { get; set; } = string.Empty;

        public override void Execute(EntityUid uid)
        {
            if (!EntityManager.TryGetComponent(uid, out AirAlarmComponent alarm)
                || !EntityManager.TryGetComponent(uid, out AtmosMonitorComponent monitor)
                || !EntityManager.TryGetComponent(uid, out AtmosAlarmableComponent alarmable))
                return;

            _devices = alarm.DeviceData;
            _monitor = monitor;
            _alarmable = alarmable;
            _alarmable.IgnoreAlarms = true;
            SetSiphon(uid);
        }

        public void Update(EntityUid uid)
        {
            if (_monitor == null
                || _alarmable == null
                || _monitor.TileGas == null)
                return;

            // just a little pointer
            var mixture = _monitor.TileGas;

            _lastPressure = mixture.Pressure;
            if (_lastPressure <= 0.2f) // anything below and it might get stuck
            {
                _alarmable.IgnoreAlarms = false;
                AirAlarmSystem.SetMode(uid, NetOwner!, AirAlarmMode.Filtering, false, false);
            }
        }

        private void SetSiphon(EntityUid uid)
        {
            foreach (var (addr, device) in _devices)
            {
                switch (device)
                {
                    case GasVentPumpData pumpData:
                        pumpData = GasVentPumpData.PanicModePreset;
                        pumpData.IgnoreAlarms = true;
                        AirAlarmSystem.SetData(uid, addr, pumpData);
                        break;
                    case GasVentScrubberData scrubberData:
                        scrubberData = GasVentScrubberData.PanicModePreset;
                        scrubberData.IgnoreAlarms = true;
                        AirAlarmSystem.SetData(uid, addr, scrubberData);
                        break;
                }
            }
        }

    }
}
