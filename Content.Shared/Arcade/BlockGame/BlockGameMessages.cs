using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.BlockGame;

public enum BlockGameVisualType
{
    GameField,
    HoldBlock,
    NextBlock
}

[Serializable, NetSerializable]
public enum BlockGameScreen
{
    Game,
    Pause,
    Gameover,
    Highscores
}
