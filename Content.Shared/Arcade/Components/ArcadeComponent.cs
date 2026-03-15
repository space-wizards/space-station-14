using Content.Shared.Arcade.Enums;
using Content.Shared.Arcade.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Arcade.Components;

/// <summary>
///
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedArcadeSystem))]
[AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class ArcadeComponent : Component
{
    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Player;

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public ArcadeGameState State;
}
