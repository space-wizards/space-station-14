using Robust.Shared.GameStates;
using Robust.Shared.Spawners;

namespace Content.Shared.TimedDespawn;

/// <summary>
///     Component that show examiner message if entity has <see cref="TimedDespawnComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ShowTimedDespawnComponent : Component;
