using System.Runtime.CompilerServices;
using Content.Shared.Arcade.Components;
using Content.Shared.Arcade.Events;
using Content.Shared.Arcade.Messages;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Arcade.Systems;

/// <summary>
///
/// </summary>
public sealed partial class SharedArcadeSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArcadeComponent, ArcadeChangedStateEvent>(OnArcadeChangedState);

        // BUI messages
        SubscribeLocalEvent<ArcadeComponent, ArcadeNewGameMessage>(OnArcadeNewGame);

        Subs.BuiEvents<ArcadeComponent>(ArcadeUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnBUIOpened);
            subs.Event<BoundUIClosedEvent>(OnBUIClosed);
        });
    }

    private void OnArcadeChangedState(Entity<ArcadeComponent> ent, ref ArcadeChangedStateEvent args)
    {
        switch (args.NewState)
        {
            case ArcadeGameState.Game:
                _audio.PlayPredicted(ent.Comp.NewGameSound, ent, args.Player);
                break;
            case ArcadeGameState.Win:
                _audio.PlayPredicted(ent.Comp.WinSound, ent, args.Player);
                break;
            case ArcadeGameState.Lose:
                _audio.PlayPredicted(ent.Comp.LoseSound, ent, args.Player);
                break;
        }
    }

    private void OnArcadeNewGame(Entity<ArcadeComponent> ent, ref ArcadeNewGameMessage args)
    {
        TryChangeGameState(ent.AsNullable(), args.Actor, ArcadeGameState.Game);
    }

    private void OnBUIOpened(Entity<ArcadeComponent> ent, ref BoundUIOpenedEvent args)
    {
        EnsureComp<ArcadePlayerComponent>(ent, out var comp);

        comp.Arcade = ent;
        Dirty(ent);
    }

    private void OnBUIClosed(Entity<ArcadeComponent> ent, ref BoundUIClosedEvent args)
    {
        RemComp<ArcadePlayerComponent>(ent);
    }

    /// <summary>
    ///
    /// </summary>
    public bool TryFinishGame(Entity<ArcadeComponent?> ent, EntityUid? player)
    {
        return TryChangeGameState(ent, player, ArcadeGameState.Win) || TryChangeGameState(ent, player, ArcadeGameState.Lose);
    }

    /// <summary>
    ///
    /// </summary>
    public bool TryChangeGameState(Entity<ArcadeComponent?> ent, EntityUid? player, ArcadeGameState gameState)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        var checkEv = new ArcadeChangeStateAttempt(player, ent.Comp.State, gameState);
        RaiseLocalEvent(ent, ref checkEv);

        if (checkEv.Cancelled)
            return false;

        var ev = new ArcadeChangedStateEvent(player, ent.Comp.State, gameState);
        RaiseLocalEvent(ent, ref ev);

        ent.Comp.State = gameState;
        DirtyField(ent, nameof(ArcadeComponent.State));

        return true;
    }

    /// <summary>
    ///
    /// </summary>
    public ArcadeGameState GetGameState(Entity<ArcadeComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return ArcadeGameState.Invalid;

        return ent.Comp.State;
    }
}
