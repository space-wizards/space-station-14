// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Content.Shared.Movement.Systems;
using Content.Shared.DeadSpace.Spiders.SpiderKnight;
using Content.Shared.DeadSpace.Spiders.SpiderKnight.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Damage;
using Content.Shared.DeadSpace.Abilities.Bloodsucker.Components;
using Content.Shared.DeadSpace.Abilities.Bloodsucker;
using Robust.Shared.Timing;
using Content.Server.Popups;

namespace Content.Server.DeadSpace.Spiders.SpiderKnight;

public sealed class SpiderKnightSystem : SharedBloodsuckerSystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpiderKnightComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<SpiderKnightComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SpiderKnightComponent, SpiderKnightActionEvent>(SetState);
        SubscribeLocalEvent<SpiderKnightComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<SpiderKnightComponent, DamageChangedEvent>(OnDamage);
        SubscribeLocalEvent<SpiderKnightComponent, RefreshMovementSpeedModifiersEvent>(OnRefresh);
    }

    private void OnComponentInit(EntityUid uid, SpiderKnightComponent component, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref component.SpiderKnightActionEntity, component.SpiderKnight, uid);
    }

    private void OnShutdown(EntityUid uid, SpiderKnightComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.SpiderKnightActionEntity);
    }

    private void OnRefresh(EntityUid uid, SpiderKnightComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.MovementSpeedMultiplier, component.MovementSpeedMultiplier);
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var spiderKnightQuery = EntityQueryEnumerator<SpiderKnightComponent>();
        while (spiderKnightQuery.MoveNext(out var ent, out var spiderKnight))
        {
            if (!spiderKnight.IsRunningState)
            {
                if (_gameTiming.CurTime > spiderKnight.TimeLeftPay)
                {
                    SetBlood(ent, spiderKnight);
                }
            }
        }
    }

    private void SetBlood(EntityUid uid, SpiderKnightComponent component)
    {
        if (!TryComp<BloodsuckerComponent>(uid, out var bloodsucker))
            return;

        if (bloodsucker.CountReagent < component.BloodCost)
        {
            _popup.PopupEntity(Loc.GetString("Недостаточно питательных веществ."), uid, uid);
            UpdateState(uid, component, true, false, false, 1f);
            return;
        }

        component.TimeLeftPay = _gameTiming.CurTime + TimeSpan.FromSeconds(1f);

        SetReagentCount(uid, -component.BloodCost, bloodsucker);
    }

    private void SetState(EntityUid uid, SpiderKnightComponent component, SpiderKnightActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<BloodsuckerComponent>(uid, out var bloodsucker))
            return;

        if (bloodsucker.CountReagent < component.BloodCost)
        {
            _popup.PopupEntity(Loc.GetString("Недостаточно питательных веществ."), uid, uid);
            return;
        }

        args.Handled = true;

        if (component.IsRunningState)
        {
            UpdateState(uid, component, false, true, false, component.MovementSpeedDebuff);
        }
        else if (component.IsDefendState)
        {
            UpdateState(uid, component, false, false, true, component.MovementBuff);
        }
        else
        {
            UpdateState(uid, component, true, false, false, 1f);
        }
    }

    private void UpdateState(EntityUid uid, SpiderKnightComponent component, bool isRunningState, bool isDefendState, bool isAttackState, float speedMultiplier)
    {
        component.IsRunningState = isRunningState;
        component.IsDefendState = isDefendState;
        component.IsAttackState = isAttackState;

        _appearance.SetData(uid, SpiderKnightVisuals.state, isRunningState);
        _appearance.SetData(uid, SpiderKnightVisuals.defend, isDefendState);
        _appearance.SetData(uid, SpiderKnightVisuals.attack, isAttackState);

        component.MovementSpeedMultiplier = speedMultiplier;
        _movement.RefreshMovementSpeedModifiers(uid);
    }

    private void OnMeleeHit(EntityUid uid, SpiderKnightComponent component, MeleeHitEvent args)
    {
        if (component.IsAttackState)
            args.BonusDamage = args.BaseDamage * component.DamageMultiply;
    }

    private void OnDamage(EntityUid uid, SpiderKnightComponent component, DamageChangedEvent args)
    {
        if (args.DamageDelta == null)
            return;

        if (component.IsDefendState)
        {

            var damage = args.DamageDelta * component.GetDamageMultiply;
            _damageable.TryChangeDamage(uid, -damage);
        }
    }

}
