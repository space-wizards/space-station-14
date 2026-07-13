using Robust.Shared.GameStates;

namespace Content.Shared.TimeStop;

/// <summary>
/// Marks an entity as immune to time stops.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TimeStopImmuneComponent : Component
{

}
