using Robust.Shared.Spawners;

namespace Content.Shared.Examine;

/// <summary>
///     Component that show examiner message if entity has <see cref="TimedDespawnComponent"/>.
/// </summary>
[RegisterComponent]
public sealed partial class ShowTimedDespawnComponent : Component;
