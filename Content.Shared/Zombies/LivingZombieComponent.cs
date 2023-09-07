using Robust.Shared.GameStates;

namespace Content.Shared.Zombies;

/// <summary>
///   Tracks any player who is alive and is currently a zombie. Simplifies queries.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedZombieSystem))]
public sealed partial class LivingZombieComponent : Component
{
}
