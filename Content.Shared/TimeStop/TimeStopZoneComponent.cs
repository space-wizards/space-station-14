using Robust.Shared.GameStates;

namespace Content.Shared.TimeStop;

/// <summary>
/// Marks an entity as a time-stop zone. Entities which enter a collision with it will be frozen in time.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TimeStopZoneComponent : Component
{
    public HashSet<EntityUid> FrozenEntities = new HashSet<EntityUid>();
}
