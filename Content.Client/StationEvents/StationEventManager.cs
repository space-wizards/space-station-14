#nullable enable
using System;
using System.Collections.Generic;
using Content.Shared.Network.NetMessages;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Content.Client.StationEvents
{
    internal sealed class StationEventManager : IStationEventManager
    {
        [Dependency] private readonly IClientNetManager _netManager = default!;

        private readonly List<string> _events = new();
        public IReadOnlyList<string> StationEvents => _events;
        public event Action? OnStationEventsReceived;

        public void Initialize()
        {
            _netManager.RegisterNetMessage<MsgRequestStationEvents>(nameof(MsgRequestStationEvents));
            _netManager.RegisterNetMessage<MsgStationEvents>(nameof(MsgStationEvents), RxStationEvents);
            _netManager.Disconnect += OnNetManagerOnDisconnect;
        }

        private void OnNetManagerOnDisconnect(object? sender, NetDisconnectedArgs msg)
        {
            _events.Clear();
        }

        private void RxStationEvents(MsgStationEvents msg)
        {
            _events.Clear();
            _events.AddRange(msg.Events);
            OnStationEventsReceived?.Invoke();
        }

        public void RequestEvents()
        {
            _netManager.ClientSendMessage(_netManager.CreateNetMessage<MsgRequestStationEvents>());
        }
    }
}
