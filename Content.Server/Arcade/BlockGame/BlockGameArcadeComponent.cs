using Robust.Shared.Player;

namespace Content.Server.Arcade.BlockGame;

[RegisterComponent]
public sealed partial class BlockGameArcadeComponent : Component
{
    /// <summary>
    /// The currently active session of NT-BG.
    /// </summary>
    public BlockGame? Game = null;

    /// <summary>
    /// The player currently playing the active session of NT-BG.
    /// </summary>
    public ICommonSession? Player = null;

    /// <summary>
    /// The players currently viewing (but not playing) the active session of NT-BG.
    /// </summary>
    public readonly List<ICommonSession> Spectators = new();
}
