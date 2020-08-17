#nullable enable
using System.Collections.Generic;

namespace Content.Client.StationEvents
{
    public interface IStationEventManager
    {
        public List<string>? StationEvents { get; }
        public void Initialize();
    }
}
