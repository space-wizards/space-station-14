using Robust.Shared.GameStates;

namespace Content.Shared.Clothing;

/// <summary>
/// Providing gravity to the wearer when on.
/// Requires <c>ItemToggleComponent</c>.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedMagbootsSystem))]
public sealed partial class MagbootsComponent : Component;
