// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Robust.Shared.Timing;
using Content.Server.DeadSpace.Sith.Components;
using Content.Shared.DeadSpace.Sith;
using Content.Shared.Popups;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction.Components;
using Content.Shared.Hands.EntitySystems;

namespace Content.Server.DeadSpace.Sith;

public sealed class SithShieldAbilitySystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SithForceShieldAbilityComponent, SithShieldEvent>(OnSithShield);
        SubscribeLocalEvent<SithForceShieldAbilityComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<SithForceShieldAbilityComponent, ComponentShutdown>(OnComponentShutdown);
    }

    public override void Update(float frameTime)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        var curTime = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<SithForceShieldAbilityComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.TimeUtil <= curTime && component.IsActiveAbility)
                DeleteShield(uid, component);
        }
    }
    
    private void OnComponentInit(EntityUid uid, SithForceShieldAbilityComponent component, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref component.ActionSithShieldEntity, component.ActionSithShield, uid);
    }

    private void OnComponentShutdown(EntityUid uid, SithForceShieldAbilityComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.ActionSithShieldEntity);
    }

    private void DeleteShield(EntityUid uid, SithForceShieldAbilityComponent component)
    {
        _hands.DoDrop(uid, component.HandShield);

        QueueDel(component.ShieldPrototype);

        component.IsActiveAbility = false;
    }

    private void OnSithShield(EntityUid uid, SithForceShieldAbilityComponent component, SithShieldEvent args)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        if (!TryComp<HandsComponent>(uid, out var hands))
        {
            _popup.PopupEntity(Loc.GetString("У вас нет рук!"), uid, uid);
            return;
        }

        if (hands.ActiveHand == null || hands.ActiveHandEntity != null)
        {
            _popup.PopupEntity(Loc.GetString("Выберете пустую руку!"), uid, uid);
            return;
        }

        if (args.Handled)
            return;

        args.Handled = true;

        var shield = Spawn(component.ShieldPrototypeId, Transform(uid).Coordinates);

        if (!HasComp<UnremoveableComponent>(shield))
            AddComp<UnremoveableComponent>(shield);

        component.ShieldPrototype = shield;

        if (!_hands.TryPickup(uid, shield, hands.ActiveHand))
        {
            QueueDel(shield);
        }
        component.HandShield = hands.ActiveHand;

        component.IsActiveAbility = true;

        component.TimeUtil = _gameTiming.CurTime + TimeSpan.FromSeconds(component.Duration);
    }
}
