using Content.Shared.Arcade.Components;
using Content.Shared.Arcade.Events;
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

        Subs.BuiEvents<ArcadeComponent>(ArcadeUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnBUIOpened);
            subs.Event<BoundUIClosedEvent>(OnBUIClosed);
        });
    }

    #region Events

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

    private void OnBUIOpened(Entity<ArcadeComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (ent.Comp.Player.HasValue)
            return;

        ent.Comp.Player = args.Actor;
        DirtyField(ent.AsNullable(), nameof(ArcadeComponent.Player));
    }

    private void OnBUIClosed(Entity<ArcadeComponent> ent, ref BoundUIClosedEvent args)
    {
        ent.Comp.Player = null;
        DirtyField(ent.AsNullable(), nameof(ArcadeComponent.Player));
    }

    #endregion

    #region API

    /// <summary>
    ///
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="gameState"></param>
    /// <returns></returns>
    public bool TryChangeGameState(Entity<ArcadeComponent?> ent, ArcadeGameState gameState)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        var checkEv = new ArcadeChangeStateAttempt(ent.Comp.Player, ent.Comp.State, gameState);
        RaiseLocalEvent(ent, ref checkEv);

        if (checkEv.Cancelled)
            return false;

        var ev = new ArcadeChangedStateEvent(ent.Comp.Player, ent.Comp.State, gameState);
        RaiseLocalEvent(ent, ref ev);

        ent.Comp.State = gameState;
        DirtyField(ent, nameof(ArcadeComponent.State));

        return true;
    }

    #endregion
}
