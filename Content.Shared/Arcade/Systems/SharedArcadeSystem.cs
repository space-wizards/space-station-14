using Content.Shared.Arcade.Components;
using Content.Shared.Arcade.Events;

namespace Content.Shared.Arcade.Systems;

/// <summary>
///
/// </summary>
public sealed partial class SharedArcadeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<ArcadeComponent>(ArcadeUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnBUIOpened);
            subs.Event<BoundUIClosedEvent>(OnBUIClosed);
        });
    }

    #region Events

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
        DirtyField(ent.AsNullable(), nameof(ArcadeComponent.State));

        return true;
    }

    #endregion
}
