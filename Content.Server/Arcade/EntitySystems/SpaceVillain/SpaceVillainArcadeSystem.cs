using Content.Server.Arcade.Components;
using Content.Server.Arcade.Components.SpaceVillain;
using Content.Shared.Arcade.SpaceVillain;
using Content.Shared.Arcade.SpaceVillain.Events;
using Content.Shared.Power;
using Robust.Server.GameObjects;

namespace Content.Server.Arcade.EntitySystems.SpaceVillain;

/// <summary>
///
/// </summary>
public sealed class SpaceVillainArcadeSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceVillainArcadeComponent, PowerChangedEvent>(OnPowerChanged);

        Subs.BuiEvents<SpaceVillainArcadeComponent>(SpaceVillainArcadeUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnBUIOpened);
            subs.Event<SpaceVillainAttackActionMessage>(OnAttackAction);
            subs.Event<SpaceVillainHealActionMessage>(OnHealAction);
            subs.Event<SpaceVillainRechargeActionMessage>(OnRechargeAction);
            subs.Event<SpaceVillainNewGameActionMessage>(OnNewGameAction);
            subs.Event<BoundUIClosedEvent>(OnBUIClosed);
        });
    }

    private void OnPowerChanged(Entity<SpaceVillainArcadeComponent> ent, ref PowerChangedEvent args)
    {
        if (args.Powered)
            return;

        _uiSystem.CloseUi(ent.Owner, SpaceVillainArcadeUiKey.Key);
    }

    private void OnBUIOpened(Entity<SpaceVillainArcadeComponent> ent, ref BoundUIOpenedEvent args)
    {
        var (uid, component) = ent;

        var message = new SpaceVillainInitialDataMessage(component.PlayerHP, component.PlayerMP, component.VillainName, component.VillainHP, component.VillainMP);
        _uiSystem.ServerSendUiMessage(uid, SpaceVillainArcadeUiKey.Key, message);
    }

    private void OnAttackAction(Entity<SpaceVillainArcadeComponent> ent, ref SpaceVillainAttackActionMessage args)
    {
        var (uid, component) = ent;

        if (component.Player != args.Actor)
            return;
    }

    private void OnHealAction(Entity<SpaceVillainArcadeComponent> ent, ref SpaceVillainHealActionMessage args)
    {
        var (uid, component) = ent;

        if (component.Player != args.Actor)
            return;
    }

    private void OnRechargeAction(Entity<SpaceVillainArcadeComponent> ent, ref SpaceVillainRechargeActionMessage args)
    {
        var (uid, component) = ent;

        if (component.Player != args.Actor)
            return;
    }

    private void OnNewGameAction(Entity<SpaceVillainArcadeComponent> ent, ref SpaceVillainNewGameActionMessage args)
    {
        var (uid, component) = ent;

        if (component.Player != args.Actor)
            return;
    }

    private void OnBUIClosed(Entity<SpaceVillainArcadeComponent> ent, ref BoundUIClosedEvent args)
    {
        var (uid, component) = ent;

        if (component.Player != args.Actor)
            return;
    }
}
