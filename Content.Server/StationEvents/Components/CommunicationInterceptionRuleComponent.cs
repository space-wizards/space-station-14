using Content.Server.StationEvents.Events;
using Content.Server.AlertLevel;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(CommunicationInterceptionRule))]
public sealed partial class CommunicationInterceptionRuleComponent : Component
{
    /// <summary>
    /// Alert level to set the station to when the event starts.
    /// </summary>

    public readonly string AlertLevel = "blue";
}
