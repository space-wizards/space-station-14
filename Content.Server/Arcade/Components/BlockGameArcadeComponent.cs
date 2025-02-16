using Content.Server.Arcade.EntitySystems.SpaceVillain;
using Content.Shared.Arcade.BlockGame;

namespace Content.Server.Arcade.Components.SpaceVillain;

/// <summary>
///
/// </summary>
[RegisterComponent, Access(typeof(BlockGameArcadeSystem))]
public sealed partial class BlockGameArcadeComponent : Component
{
    /// <summary>
    ///
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public BlockGameArcadeBlock[] DefaultField = [];

    /// <summary>
    ///
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public BlockGameArcadeBlock[] CurrentField = [];
}
