#nullable enable
using System;
using System.Collections.Generic;
using Content.Shared.StationEvents;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;

namespace Content.Client.StationEvents
{
    class StationEventManager : SharedStationEvent, IStationEventManager
    {
        private List<string>? _events;
        public List<string>? StationEvents
        {
            get
            {
                if (_events == null)
                    RequestEvents();
                return _events;
            }
        }
        public event Action? OnStationEventsReceived;

        public void Initialize()
        {
            var netManager = IoCManager.Resolve<IClientNetManager>();
            netManager.RegisterNetMessage<MsgGetStationEvents>(nameof(MsgGetStationEvents), EventHandler);
            netManager.Disconnect += (sender, msg) => _events = null;
        }

        private void EventHandler(MsgGetStationEvents msg)
        {
            _events = msg.Events;
            OnStationEventsReceived?.Invoke();
        }
        public void RequestEvents()
        {
            var netManager = IoCManager.Resolve<IClientNetManager>();
            netManager.ClientSendMessage(netManager.CreateNetMessage<MsgGetStationEvents>());
        }
    }
}
