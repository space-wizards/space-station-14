using System;
using System.Collections.Generic;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Monitor.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Monitor.Components
{
    [RegisterComponent]
    public class AirAlarmComponent : Component
    {
        [ComponentDependency] public readonly ApcPowerReceiverComponent? DeviceRecvComponent = default!;
        [ComponentDependency] public readonly AtmosMonitorComponent? AtmosMonitorComponent = default!;
        [Dependency] private readonly AirAlarmSystem? _airAlarmSystem = default!;

        [ViewVariables] private BoundUserInterface? _userInterface;

        public override string Name => "AirAlarm";

        // cache of device data
        public Dictionary<string, IAtmosDeviceData> DeviceData = new();

        public AirAlarmMode CurrentMode = AirAlarmMode.Filtering;

        protected override void Initialize()
        {
            base.Initialize();
            IoCManager.InjectDependencies(this);
            _userInterface = Owner.GetUIOrNull(SharedAirAlarmInterfaceKey.Key);
            if (_userInterface != null)
                _userInterface.OnReceiveMessage += OnMessageReceived;
        }

        public void UpdateUI()
        {
            var gas = AtmosMonitorComponent != null ? AtmosMonitorComponent.TileGas : null;
            Dictionary<Gas, float> gasInTile = new();

            if (gas != null)
                foreach (var gasType in Enum.GetValues<Gas>())
                    gasInTile.Add(gasType, gas.GetMoles(gasType));

            _userInterface?.SetState(new AirAlarmBoundUserInterfaceState
            {
                Pressure = gas != null ? gas.Pressure : null,
                Temperature = gas != null ? gas.Temperature : null,
                Gases = gasInTile,
                TotalMoles = gas != null ? gas.TotalMoles : null,
                DeviceData = DeviceData,
                CurrentMode = CurrentMode
            });
        }

        public void OnMessageReceived(ServerBoundUserInterfaceMessage message)
        {
            if (_airAlarmSystem != null)
                switch (message.Message)
                {
                    case AirAlarmChangeDeviceDataMessage deviceData:
                        _airAlarmSystem.SetData(Owner.Uid, deviceData.Address, deviceData.Data);
                        break;
                    case AirAlarmChangeModeMessage modeData:
                        CurrentMode = modeData.Mode;
                        // TODO: send update to air alarm system so it does something
                        break;
                }
        }
    }

    public class AirAlarmModeProgram
    {
        public List<string> TurnDeviceOn = new();
        public List<string> TurnDeviceOff = new();
    }



    // similar to SS13 air alarm modes

}
