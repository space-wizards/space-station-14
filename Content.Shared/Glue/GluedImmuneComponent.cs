using Robust.Shared.GameStates;

namespace Content.Shared.Glue;

/// <summary>
/// This component indicates that an entity ignores the <see cref="GluedComponent"/> on items.
/// </summary>
// Careful adding this to player content! It has strong design implications!
[RegisterComponent, NetworkedComponent]
[Access(typeof(GlueSystem))]
public sealed partial class GluedImmuneComponent : Component
{

}
