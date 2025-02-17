// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Stunnable;
using Content.Shared.DeadSpace.Demons.Abilities.Components;
using Content.Shared.DeadSpace.Demons.Abilities;
using System.Linq;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server.DeadSpace.Demons.Abilities;

public sealed partial class StunAttackSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StunAttackComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<StunAttackComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<StunAttackComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<StunAttackComponent, StunAttackActionEvent>(DoStunAttack);
    }

    private void OnComponentInit(EntityUid uid, StunAttackComponent component, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref component.ActionStunAttackEntity, component.ActionStunAttack, uid);
    }

    private void OnComponentShutdown(EntityUid uid, StunAttackComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.ActionStunAttackEntity);
    }

    private void DoStunAttack(EntityUid uid, StunAttackComponent component, StunAttackActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        component.IsStunAttack = true;
    }

    private void OnMeleeHit(EntityUid uid, StunAttackComponent component, MeleeHitEvent args)
    {
        if (!args.HitEntities.Any())
            return;

        if (!component.IsStunAttack)
            return;

        component.IsStunAttack = false;

        foreach (var entity in args.HitEntities)
        {
            if (args.User == entity)
                continue;

            if (!TryComp<MobStateComponent>(entity, out var mobState))
                continue;


            if (mobState.CurrentState == MobState.Alive)
            {
                _damageable.TryChangeDamage(uid, component.HealingOnBite, true, false);
            }

            if (TryComp(entity, out PhysicsComponent? physics))
            {
                _physics.SetLinearVelocity(entity, physics.LinearVelocity * component.LaunchForwardsMultiplier, body: physics);
            }

            _stun.TryParalyze(entity, TimeSpan.FromSeconds(component.ParalyzeTime), true);
        }
    }
}
