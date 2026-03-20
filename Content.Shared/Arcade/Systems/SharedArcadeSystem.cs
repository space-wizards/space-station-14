using Content.Shared.Arcade.Components;
using Content.Shared.Arcade.Enums;
using Content.Shared.Arcade.Events;
using Content.Shared.Arcade.Messages;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Arcade.Systems;

/// <summary>
///
/// </summary>
public sealed partial class SharedArcadeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArcadeEmitSoundOnNewGameComponent, ArcadeChangedStateEvent>(OnArcadeChangedStateNewGame);
        SubscribeLocalEvent<ArcadeEmitSoundOnWinComponent, ArcadeChangedStateEvent>(OnArcadeChangedStateWin);
        SubscribeLocalEvent<ArcadeEmitSoundOnLoseComponent, ArcadeChangedStateEvent>(OnArcadeChangedStateLose);

        // BUI messages
        SubscribeLocalEvent<ArcadeComponent, ArcadeNewGameMessage>(OnArcadeNewGame);

        Subs.BuiEvents<ArcadeComponent>(ArcadeUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnBUIOpened);
            subs.Event<BoundUIClosedEvent>(OnBUIClosed);
        });
    }

    private void OnArcadeChangedStateNewGame(Entity<ArcadeEmitSoundOnNewGameComponent> ent, ref ArcadeChangedStateEvent args)
    {
        if (args.NewState != ArcadeGameState.NewGame)
            return;

        _audio.PlayPredicted(ent.Comp.Sound, ent, args.Player);
    }

    private void OnArcadeChangedStateWin(Entity<ArcadeEmitSoundOnWinComponent> ent, ref ArcadeChangedStateEvent args)
    {
        if (args.NewState != ArcadeGameState.Win)
            return;

        _audio.PlayPredicted(ent.Comp.Sound, ent, args.Player);
    }

    private void OnArcadeChangedStateLose(Entity<ArcadeEmitSoundOnLoseComponent> ent, ref ArcadeChangedStateEvent args)
    {
        if (args.NewState != ArcadeGameState.Lose)
            return;

        _audio.PlayPredicted(ent.Comp.Sound, ent, args.Player);
    }

    private void OnArcadeNewGame(Entity<ArcadeComponent> ent, ref ArcadeNewGameMessage args)
    {
        if (ent.Comp.Player != args.Actor)
            return;

        TryChangeGameState(ent, ArcadeGameState.NewGame);
    }

    private void OnBUIOpened(Entity<ArcadeComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (ent.Comp.Player.HasValue)
            return;

        ent.Comp.Player = args.Actor;
        DirtyField(ent.AsNullable(), nameof(ArcadeComponent.Player));
    }

    private void OnBUIClosed(Entity<ArcadeComponent> ent, ref BoundUIClosedEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (ent.Comp.Player != args.Actor)
            return;

        _ui.CloseUi(ent.Owner, ArcadeUiKey.Key);

        ent.Comp.Player = null;
        DirtyField(ent.AsNullable(), nameof(ArcadeComponent.Player));
    }

    /// <summary>
    ///
    /// </summary>
    public bool TryChangeGameState(Entity<ArcadeComponent> ent, ArcadeGameState newState)
    {
        if (ent.Comp.State == newState)
            return false;

        var ev = new ArcadeChangedStateEvent(ent.Comp.Player, ent.Comp.State, newState);
        RaiseLocalEvent(ent, ref ev);

        if (newState == ArcadeGameState.NewGame)
            return TryChangeGameState(ent, ArcadeGameState.Game);

        ent.Comp.State = newState;
        DirtyField(ent.AsNullable(), nameof(ArcadeComponent.State));

        return true;
    }
}
