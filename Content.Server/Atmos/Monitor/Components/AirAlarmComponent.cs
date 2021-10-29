using System;
using System.Collections.Generic;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared.Atmos;
using Content.Shared.Interaction;
using Content.Shared.Atmos.Monitor.Components;
using Content.Shared.Atmos.Monitor.Systems;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Monitor.Components
{
    [RegisterComponent]
    public class AirAlarmComponent : Component, IInteractHand
    {
        [ComponentDependency] public readonly ApcPowerReceiverComponent? DeviceRecvComponent = default!;
        [ComponentDependency] public readonly AtmosMonitorComponent? AtmosMonitorComponent = default!;
        [ComponentDependency] public readonly AirAlarmDataComponent? AirAlarmDataComponent = default!;

        private AirAlarmSystem? _airAlarmSystem = default!;
        private AirAlarmDataSystem? _airAlarmDataSystem = default!;

        [ViewVariables] private BoundUserInterface? _userInterface;

        public override string Name => "AirAlarm";

        protected override void Initialize()
        {
            base.Initialize();

            _airAlarmSystem = EntitySystem.Get<AirAlarmSystem>();
            _airAlarmDataSystem = EntitySystem.Get<AirAlarmDataSystem>();
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
            _activePlayers.Add(player.UserId);
            if (_airAlarmSystem != null) // if this is null you got a lot of other shit to deal with
            {
                _airAlarmSystem.AddActiveInterface(Owner.Uid);
                _airAlarmSystem.UpdateAirData(Owner.Uid);
                _airAlarmSystem.SendAlarmMode(Owner.Uid);
                _airAlarmSystem.SendThresholds(Owner.Uid);
            }
            _userInterface?.Open(player);
        }

        private void OnCloseUI(IPlayerSession player)
        {
            _activePlayers.Remove(player.UserId);
            if (_airAlarmSystem != null && _activePlayers.Count == 0)
                _airAlarmSystem.RemoveActiveInterface(Owner.Uid);
        }

        public void UpdateUI()
        {
            // send it literally the data component's owner uid, similar to an event
            // UI side will look at the component and set state from there
            if (AirAlarmDataComponent != null)
                _userInterface?.SetState(new AirAlarmBoundUserInterfaceState(AirAlarmDataComponent.Owner.Uid));
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            var session = eventArgs.User.PlayerSession();

            if (session == null) return false;

            OpenUI(session);

            return true;
        }

        public void OnMessageReceived(ServerBoundUserInterfaceMessage message)
        {
            if (_airAlarmDataSystem == null) return;

            switch (message.Message)
            {
                case AirAlarmUpdateAlarmModeMessage alarmMessage:
                    _airAlarmDataSystem.UpdateAlarmMode(Owner.Uid, alarmMessage.Mode);
                    break;
                case AirAlarmUpdateAlarmThresholdMessage thresholdMessage:
                    _airAlarmDataSystem.UpdateAlarmThreshold(Owner.Uid, thresholdMessage.Threshold, thresholdMessage.Type, thresholdMessage.Gas);
                    break;
                case AirAlarmUpdateDeviceDataMessage dataMessage:
                    _airAlarmDataSystem.UpdateDeviceData(Owner.Uid, dataMessage.Address, dataMessage.Data);
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
