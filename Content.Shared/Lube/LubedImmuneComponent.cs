using Robust.Shared.GameStates;

namespace Content.Shared.Lube;

/// <summary>
/// This component indicates that an entity ignores the <see cref="LubedComponent"/> on items.
/// </summary>
// Careful adding this to player content! It has strong design implications!
[RegisterComponent, NetworkedComponent]
[Access(typeof(LubedSystem))]
public sealed partial class LubedImmuneComponent : Component
{

}
