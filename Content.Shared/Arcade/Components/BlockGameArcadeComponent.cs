using Content.Shared.Arcade.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Arcade.Components;

/// <summary>
///
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedBlockGameArcadeSystem))]
public sealed partial class BlockGameArcadeComponent : Component
{

}
