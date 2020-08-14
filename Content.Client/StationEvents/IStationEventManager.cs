#nullable enable
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Client.StationEvents
{
    public interface IStationEventManager
    {
        public List<string>? StationEvents { get; }
        public void Initialize();
    }
}
