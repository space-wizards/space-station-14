using Content.Server.StationEvents.Events;

namespace Content.Server.StationEvents.Components;

/// <summary>
/// Configuration component for the Thief antag spawning rule.
/// </summary>
[RegisterComponent, Access(typeof(ThiefSpawnRule))]
public sealed partial class ThiefSpawnRuleComponent : Component
{
    [DataField]
    public int ThiefCount = 2;
}
