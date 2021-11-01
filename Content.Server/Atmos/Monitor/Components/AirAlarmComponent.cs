using System;
using System.Linq;
using System.Collections.Generic;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared.Atmos;
using Content.Shared.Interaction;
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
    public class AirAlarmComponent : Component, IInteractHand
    {
        [ComponentDependency] public readonly ApcPowerReceiverComponent? DeviceRecvComponent = default!;
        [ComponentDependency] public readonly AtmosMonitorComponent? AtmosMonitorComponent = default!;

        private AirAlarmSystem? _airAlarmSystem = default!;

        [ViewVariables] private BoundUserInterface? _userInterface;

        [ViewVariables] public AirAlarmMode CurrentMode { get; set; }

        public override string Name => "AirAlarm";

        protected override void Initialize()
        {
            base.Initialize();

            _airAlarmSystem = EntitySystem.Get<AirAlarmSystem>();
            _userInterface = Owner.GetUIOrNull(SharedAirAlarmInterfaceKey.Key);
            if (_userInterface != null)
            {
                _userInterface.OnReceiveMessage += OnMessageReceived;
                _userInterface.OnClosed += OnCloseUI;
            }
        }

        private HashSet<NetUserId> _activePlayers = new();

        public bool HasPlayers() => _activePlayers.Any();

        public void SendMessage(BoundUserInterfaceMessage message)
        {
            if (_userInterface != null)
                _userInterface.SendMessage(message);
        }

        public void OpenUI(IPlayerSession player)
        {
            _activePlayers.Add(player.UserId);
            _userInterface?.Open(player);
            if (_airAlarmSystem != null) // if this is null you got a lot of other shit to deal with
            {
                _airAlarmSystem.AddActiveInterface(Owner.Uid);
                _airAlarmSystem.SendAlarmMode(Owner.Uid);
                _airAlarmSystem.SendThresholds(Owner.Uid);
                _airAlarmSystem.SyncAllDevices(Owner.Uid); // this should honestly be a button
                _airAlarmSystem.SendAirData(Owner.Uid);
            }
        }

        private void OnCloseUI(IPlayerSession player)
        {
            _activePlayers.Remove(player.UserId);
            if (_airAlarmSystem != null && _activePlayers.Count == 0)
                _airAlarmSystem.RemoveActiveInterface(Owner.Uid);
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
            if (_airAlarmSystem == null) return;

            switch (message.Message)
            {
                case AirAlarmUpdateAlarmModeMessage alarmMessage:
                    _airAlarmSystem.SetMode(Owner.Uid, alarmMessage.Mode);
                    break;
                case AirAlarmUpdateAlarmThresholdMessage thresholdMessage:
                    _airAlarmSystem.SetThreshold(Owner.Uid, thresholdMessage.Threshold, thresholdMessage.Type, thresholdMessage.Gas);
                    break;
                case AirAlarmUpdateDeviceDataMessage dataMessage:
                    _airAlarmSystem.SetDeviceData(Owner.Uid, dataMessage.Address, dataMessage.Data);
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
