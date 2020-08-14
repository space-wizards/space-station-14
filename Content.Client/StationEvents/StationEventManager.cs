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
        }

        public void Initialize()
        {
            var netManager = IoCManager.Resolve<IClientNetManager>();
            netManager.RegisterNetMessage<MsgGetStationEvents>(nameof(MsgGetStationEvents),
                msg => _events = msg.Events);
            netManager.Disconnect += (sender, msg) => _events = null;
        }

        public void RequestEvents()
        {
            IoCManager.Resolve<IClientNetManager>().ClientSendMessage(IoCManager.Resolve<IClientNetManager>().CreateNetMessage<MsgGetStationEvents>());
        }
    }
}
