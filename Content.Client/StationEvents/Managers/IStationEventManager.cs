using System;
using System.Collections.Generic;

namespace Content.Client.StationEvents.Managers
{
    public interface IStationEventManager
    {
        public IReadOnlyList<string> StationEvents { get; }
        public void Initialize();
        public event Action OnStationEventsReceived;
        public void RequestEvents();
    }
}
