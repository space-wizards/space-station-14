using Content.Server.Arcade.Components.SpaceVillain;
using Content.Server.Power.EntitySystems;
using Content.Shared.Arcade.BlockGame;
using Content.Shared.Arcade.BlockGame.Events;
using Content.Shared.Power;
using Robust.Server.GameObjects;

namespace Content.Server.Arcade.EntitySystems.SpaceVillain;

/// <summary>
///
/// </summary>
public sealed class BlockGameArcadeSystem : EntitySystem
{
    [Dependency] private readonly PowerReceiverSystem _powerReceiverSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly ArcadeSystem _arcadeSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlockGameArcadeComponent, PowerChangedEvent>(OnPowerChanged);

        Subs.BuiEvents<BlockGameArcadeComponent>(BlockGameArcadeUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnBUIOpened);
            subs.Event<BlockGameNewGameActionMessage>(OnNewGameAction);
            subs.Event<BoundUIClosedEvent>(OnBUIClosed);
        });
    }

    private void OnPowerChanged(Entity<BlockGameArcadeComponent> ent, ref PowerChangedEvent args)
    {
        if (args.Powered)
            return;

        _uiSystem.CloseUi(ent.Owner, BlockGameArcadeUiKey.Key);
    }

    private void OnBUIOpened(Entity<BlockGameArcadeComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!_powerReceiverSystem.IsPowered(ent.Owner))
            return;

        if (_arcadeSystem.GetPlayer(ent) == null)
            _arcadeSystem.SetPlayer(ent, args.Actor);

        SendData(ent, args.Actor);
    }

    private void OnNewGameAction(Entity<BlockGameArcadeComponent> ent, ref BlockGameNewGameActionMessage args)
    {
        var component = ent.Comp;

        if (_arcadeSystem.GetPlayer(ent) != args.Actor || !_powerReceiverSystem.IsPowered(ent.Owner))
            return;

        _arcadeSystem.PlayNewGameSound(ent);
    }

    private void OnBUIClosed(Entity<BlockGameArcadeComponent> ent, ref BoundUIClosedEvent args)
    {
        if (_arcadeSystem.GetPlayer(ent) != args.Actor)
            return;

        _arcadeSystem.SetPlayer(ent, null);

        _uiSystem.CloseUi(ent.Owner, BlockGameArcadeUiKey.Key);
    }

    /// <summary>
    ///
    /// </summary>
    private void SendData(Entity<BlockGameArcadeComponent> ent, EntityUid? actor = null)
    {
        var component = ent.Comp;
    }

    /// <summary>
    ///
    /// </summary>
    private bool UpdateGameState(Entity<BlockGameArcadeComponent> ent)
    {
        var component = ent.Comp;


        return true;
    }

    /// <summary>
    ///
    /// </summary>
    private bool CanUseAction(Entity<BlockGameArcadeComponent> ent, EntityUid actor)
    {
        var component = ent.Comp;

        if (_arcadeSystem.GetPlayer(ent) != actor || !_powerReceiverSystem.IsPowered(ent.Owner))
            return false;

        return true;
    }
}
