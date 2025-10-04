using static Content.Shared.Arcade.MineGameShared;

namespace Content.Server.Arcade.MineGame;

public sealed partial class MineGame
{
    /// <summary>
    /// Updates the UI.
    /// </summary>
    public void UpdateUi(EntityUid uid, MineGameTileVisState[,]? updatedTiles)
    {
        _uiSystem.ServerSendUiMessage(uid, MineGameArcadeUiKey.Key, GenerateBoardUpdateMessage(updatedTiles));
    }

    /// <summary>
    /// Generates an array of tile visual states based on the current game board state, and packs width
    /// and other metadata like game time/status/mine count into update message.
    /// </summary>
    /// <returns>A mine game board update message.</returns>
    public MineGameBoardUpdateMessage GenerateBoardUpdateMessage(MineGameTileVisState[,]? updatedTiles = null)
    {
        var tileStates = new MineGameTileVisState[_boardSettings.BoardSize.X * _boardSettings.BoardSize.Y];
        var readFrom = updatedTiles ?? _tileVisState; // Send entire board if we aren't sending a specific update set

        for (var y = 0; y < _boardSettings.BoardSize.Y; ++y)
            for (var x = 0; x < _boardSettings.BoardSize.X; ++x)
                tileStates[y * _boardSettings.BoardSize.X + x] = readFrom[x, y];

        MineGameMetadata metadata = new(_referenceTime, _minesGenerated && !(GameWon || GameLost), _boardSettings.MineCount - _flagCount);
        return new(_boardSettings.BoardSize.X, tileStates, metadata);
    }
}
