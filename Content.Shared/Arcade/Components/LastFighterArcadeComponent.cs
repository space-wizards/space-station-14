using Content.Shared.Arcade.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Arcade.Components;

/// <summary>
///
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedLastFighterArcadeSystem))]
public sealed partial class LastFighterArcadeComponent : Component
{

}
