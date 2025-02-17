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
    public BlockGameArcadeBlock[] PreviewField = new BlockGameArcadeBlock[9];

    /// <summary>
    ///
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public BlockGameArcadeBlock[] MainField = new BlockGameArcadeBlock[100];
}
