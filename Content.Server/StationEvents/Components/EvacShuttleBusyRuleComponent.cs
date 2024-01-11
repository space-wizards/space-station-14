using System.Threading;
using Content.Server.StationEvents.Events;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(EvacShuttleBusyRule))]
public sealed partial class EvacShuttleBusyRuleComponent : Component
{
    public int NumberPerSecond = 0;
    public float UpdateRate => 1.0f / NumberPerSecond;
    public float FrameTimeAccumulator = 0.0f;
}
