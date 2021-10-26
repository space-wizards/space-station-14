using System;
using System.Collections.Generic;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Monitor.Components;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Monitor.Components
{
    [RegisterComponent]
    public class AirAlarmComponent : Component
    {
        [ComponentDependency] public readonly ApcPowerReceiverComponent? DeviceRecvComponent = default!;
        [ComponentDependency] public readonly AtmosMonitorComponent? AtmosMonitorComponent = default!;
        [ComponentDependency] public readonly AirAlarmDataComponent? AirAlarmDataComponent = default!;
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
            {
                _userInterface.OnReceiveMessage += OnMessageReceived;
                _userInterface.OnClosed += OnCloseUI;
            }
        }

        private HashSet<NetUserId> _activePlayers = new();

        public void OpenUI(IPlayerSession player)
        {
            _userInterface?.Open(player);
            _activePlayers.Add(player.UserId);
            if (_airAlarmSystem != null) // if this is null you got a lot of other shit to deal with
            {
                _airAlarmSystem.AddActiveInterface(Owner.Uid);
                _airAlarmSystem.UpdateInterfaceData(Owner.Uid);
            }

        }

        private void OnCloseUI(IPlayerSession player)
        {
            _activePlayers.Remove(player.UserId);
            if (_airAlarmSystem != null && _activePlayers.Count == 0)
                _airAlarmSystem.RemoveActiveInterface(Owner.Uid);
        }

        public void UpdateUI()
        {
            /*
            var gas = AtmosMonitorComponent != null ? AtmosMonitorComponent.TileGas : null;
            Dictionary<Gas, float> gasInTile = new();

            if (gas != null)
                foreach (var gasType in Enum.GetValues<Gas>())
                    gasInTile.Add(gasType, gas.GetMoles(gasType));
                    */

            // send it literally nothing, similar to an event
            // UI side will look at the component and set state from there
            _userInterface?.SetState(new AirAlarmBoundUserInterfaceState());
        }

        public void OnMessageReceived(ServerBoundUserInterfaceMessage message)
        {}
    }

    public class AirAlarmModeProgram
    {
        public List<string> TurnDeviceOn = new();
        public List<string> TurnDeviceOff = new();
    }



    // similar to SS13 air alarm modes

}
