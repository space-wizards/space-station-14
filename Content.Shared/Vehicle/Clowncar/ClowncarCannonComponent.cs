using Robust.Shared.GameStates;

namespace Content.Shared.Vehicle.Clowncar;

/// <summary>
/// Dummy component to mark the clowncar gun and handle events raised on it
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedClowncarSystem))]
public sealed class ClowncarCannonComponent : Component
{
}
