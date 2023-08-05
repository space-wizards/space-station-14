using Content.Server.StationEvents.Events;

namespace Content.Server.StationEvents.Components;

/// <summary>
/// Added to the station to prevent its APCs from turning on until event end
/// </summary>
[RegisterComponent, Access(typeof(PowerGridCheckRule))]
public sealed class PowerGridCheckDisabledComponent : Component
{
}
