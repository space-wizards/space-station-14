using Content.Server.Power.Components;
using Content.Shared.UserInterface;
using Content.Shared.Arcade;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using static Content.Shared.Arcade.MineGameShared;

namespace Content.Server.Arcade.MineGame;

public sealed partial class MineGameArcadeSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;



    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MineGameArcadeComponent, AfterActivatableUIOpenEvent>(OnAfterUIOpen);
        SubscribeLocalEvent<MineGameArcadeComponent, PowerChangedEvent>(OnPowerChanged);

        Subs.BuiEvents<MineGameArcadeComponent>(MineGameArcadeUiKey.Key, subs =>
        {
            subs.Event<MineGameTileActionMessage>(OnMineGameTileAction);
            subs.Event<MineGameRequestDataMessage>(OnMineGameRequestDataMessage);
            subs.Event<MineGameRequestNewBoardMessage>(OnMineGameRequestNewBoardMessage);
        });
    }

    private void OnMineGameTileAction(EntityUid uid, MineGameArcadeComponent component, MineGameTileActionMessage msg)
    {
        if (component.Game == null)
            return;
        if (!TryComp<ApcPowerReceiverComponent>(uid, out var power) || !power.Powered)
            return;

        if (component.Game.GameLost)
            return;

        component.Game.ExecutePlayerAction(uid, msg.TileAction);

        if (component.Game.GameLost)
            _audioSystem.PlayPvs(component.GameOverSound, uid, AudioParams.Default.WithVolume(-5f));
    }

    /// <summary>
    /// Generates an array of tile visual states based on the current game board state, and packs width
    /// and other metadata like game time/status/mine count.
    /// </summary>
    /// <param name="component">The arcade component hosting the game.</param>
    /// <param name="boardSize">The size of the mine game board.</param>
    /// <param name="mineCount">The number of mines to put in the mine game.</param>
    /// <returns>An initialized mine game state.</returns>
    private MineGame SetupMineGame(MineGameArcadeComponent component, Vector2i boardSize, int mineCount)
    {
        // Validate what are potentially user-inputted numbers
        boardSize = Vector2i.ComponentMin(Vector2i.ComponentMax(boardSize, component.MinBoardSize), component.MaxBoardSize);
        return new(
            boardSize,
            Math.Clamp(mineCount, component.MinMineCount, boardSize.X * boardSize.Y),
            component.SafeStartRadius
        );

    }

    private void OnMineGameRequestDataMessage(EntityUid uid, MineGameArcadeComponent component, MineGameRequestDataMessage msg)
    {
        if (component.Game == null)
            return;
        component.Game.UpdateUi(uid, null);
    }

    private void OnMineGameRequestNewBoardMessage(EntityUid uid, MineGameArcadeComponent component, MineGameRequestNewBoardMessage msg)
    {
        component.Game = SetupMineGame(component, msg.Settings.BoardSize, msg.Settings.MineCount);
        component.Game.UpdateUi(uid, null);
    }

    private void OnAfterUIOpen(EntityUid uid, MineGameArcadeComponent component, AfterActivatableUIOpenEvent args)
    {
        component.Game ??= SetupMineGame(component, component.MinBoardSize, component.MinBoardSize.X * component.MinBoardSize.Y / 8);
        component.Game.UpdateUi(uid, null);
    }

    private void OnPowerChanged(EntityUid uid, MineGameArcadeComponent component, ref PowerChangedEvent args)
    {
        if (args.Powered)
            return;

        _uiSystem.CloseUi(uid, MineGameShared.MineGameArcadeUiKey.Key);
    }
}
