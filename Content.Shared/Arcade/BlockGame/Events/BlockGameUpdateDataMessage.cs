using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.BlockGame.Events;

/// <summary>
///
/// </summary>
[Serializable, NetSerializable]
public sealed class BlockGameUpdateDataMessage(BlockGameArcadeBlock[] preview, BlockGameArcadeBlock[] main) : BoundUserInterfaceMessage
{
    /// <summary>
    ///
    /// </summary>
    public BlockGameArcadeBlock[] Preview = preview;

    /// <summary>
    ///
    /// </summary>
    public BlockGameArcadeBlock[] Main = main;
}
