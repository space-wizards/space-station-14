using Content.Shared.Arcade.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Arcade.Components;

/// <summary>
///
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedArcadeSystem))]
[AutoGenerateComponentState]
public sealed partial class ArcadePlayerComponent : Component
{
    /// <summary>
    ///
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public EntityUid? Arcade;
}
