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
    /// <param name="boardSettings">The settings (board width/height/mine count) for the new board.</param>
    /// <returns>An initialized mine game state.</returns>
    private MineGame SetupMineGame(MineGameArcadeComponent component, MineGameBoardSettings boardSettings)
    {
        // Validate what are potentially user-inputted numbers
        var clampedBoardSize = Vector2i.ComponentMin(Vector2i.ComponentMax(boardSettings.BoardSize, component.MinBoardSize),
            component.MaxBoardSize);
        var validatedBoardSettings = new MineGameBoardSettings(
            clampedBoardSize,
            Math.Clamp(boardSettings.MineCount, component.MinMineCount, clampedBoardSize.X * clampedBoardSize.Y)
        );

        return new(
            validatedBoardSettings,
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
        component.Game = SetupMineGame(component, msg.Settings);
        component.Game.UpdateUi(uid, null);
    }

    private void OnAfterUIOpen(EntityUid uid, MineGameArcadeComponent component, AfterActivatableUIOpenEvent args)
    {
        component.Game ??= SetupMineGame(component,
            new(component.MinBoardSize, component.MinBoardSize.X * component.MinBoardSize.Y / 8));
        component.Game.UpdateUi(uid, null);
    }

    private void OnPowerChanged(EntityUid uid, MineGameArcadeComponent component, ref PowerChangedEvent args)
    {
        if (args.Powered)
            return;

        _uiSystem.CloseUi(uid, MineGameShared.MineGameArcadeUiKey.Key);
    }
}
