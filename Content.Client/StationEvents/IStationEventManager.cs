#nullable enable
using System;
using System.Collections.Generic;

namespace Content.Client.StationEvents
{
    public interface IStationEventManager
    {
        public List<string>? StationEvents { get; }
        public void Initialize();
        public event Action OnStationEventsReceived;
        public void RequestEvents();
    }
}
