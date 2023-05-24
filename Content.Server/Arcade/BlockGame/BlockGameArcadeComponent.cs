using Robust.Server.Player;

namespace Content.Server.Arcade.BlockGame
{
    [RegisterComponent]
    public sealed class BlockGameArcadeComponent : Component
    {
        public BlockGame Game = default!;
        public IPlayerSession? Player = null;
        public readonly List<IPlayerSession> Spectators = new();
    }
}
