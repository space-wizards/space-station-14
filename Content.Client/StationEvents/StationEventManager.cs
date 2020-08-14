#nullable enable
using Content.Shared.StationEvents;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Client.StationEvents
{
    class StationEventManager : SharedStationEvent, IStationEventManager
    {
#pragma warning disable 649
        [Dependency] private readonly IClientNetManager _netManager = default!;
#pragma warning restore 649
        private List<string>? _events;
        public List<string>? StationEvents
        {
            get
            {
                if (_events == null)
                    RequestEvents();
                return _events;
            }
            set => _events = value;
        }

        public void Initialize()
        {
            _netManager.RegisterNetMessage<MsgGetStationEvents>(nameof(MsgGetStationEvents),
                msg => StationEvents = msg.Events);
            _netManager.Disconnect += (sender, msg) => StationEvents = null;
        }

        private void RequestEvents()
        {
            _netManager.ClientSendMessage(_netManager.CreateNetMessage<MsgGetStationEvents>());
        }
    }
}
