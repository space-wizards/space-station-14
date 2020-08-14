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
            var netManager = IoCManager.Resolve<IClientNetManager>();
            netManager.RegisterNetMessage<MsgGetStationEvents>(nameof(MsgGetStationEvents),
                msg => StationEvents = msg.Events);
            netManager.Disconnect += (sender, msg) => StationEvents = null;
        }

        private void RequestEvents()
        {
            IoCManager.Resolve<IClientNetManager>().ClientSendMessage(IoCManager.Resolve<IClientNetManager>().CreateNetMessage<MsgGetStationEvents>());
        }
    }
}
