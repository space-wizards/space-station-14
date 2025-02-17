// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Content.Server.DeadSpace.Arkalyse.Components;
using Content.Shared.Weapons.Melee.Events;
using System.Linq;
using Content.Shared.Damage;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Physics.Components;
using System.Numerics;
using Content.Shared.DeadSpace.Arkalyse;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;

namespace Content.Server.DeadSpace.Arkalyse;

public sealed class ArkalyseDamageSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionSystem = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ArkalyseDamageComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ArkalyseDamageComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<ArkalyseDamageComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<ArkalyseDamageComponent, DamageAtackArkalyseActionEvent>(OnActionActivated);

    }
    private void OnComponentInit(EntityUid uid, ArkalyseDamageComponent component, ComponentInit args)
    {
        _actionSystem.AddAction(uid, ref component.ActionDamageArkalyseAttackEntity, component.ActionDamageArkalyseAttack, uid);
    }
    private void OnComponentShutdown(EntityUid uid, ArkalyseDamageComponent component, ComponentShutdown args)
    {
        _actionSystem.RemoveAction(uid, component.ActionDamageArkalyseAttackEntity);
    }
    private void OnActionActivated(EntityUid uid, ArkalyseDamageComponent component, DamageAtackArkalyseActionEvent args)
    {
        if (args.Handled)
            return;
        component.IsDamageAttack = !component.IsDamageAttack;
        args.Handled = true;
    }
    private void OnMeleeHit(EntityUid uid, ArkalyseDamageComponent component, MeleeHitEvent args)
    {
        if (component.IsDamageAttack && args.HitEntities.Any())
        {
            foreach (var entity in args.HitEntities)
            {
                if (args.User == entity)
                    continue;

                if (!TryComp<MobStateComponent>(entity, out var mobState))
                    continue;

                if (mobState.CurrentState == MobState.Alive)
                {
                    _damageable.TryChangeDamage(entity, component.Damage, true, false);
                    _audio.PlayPvs(component.HitSound, args.User, AudioParams.Default.WithVolume(0.5f));
                }
                if (TryComp<PhysicsComponent>(entity, out var physicsComponent))
                {
                    var userTransform = Transform(args.User);
                    var targetTransform = Transform(entity);
                    var pushDirection = targetTransform.WorldPosition - userTransform.WorldPosition;

                    if (!pushDirection.Equals(Vector2.Zero))
                    {
                        var distance = pushDirection.Length();

                        if (distance <= component.MaxPushDistance)
                        {
                            pushDirection = pushDirection.Normalized();
                            var pushStrength = component.PushStrength;

                            if (component.UseDistanceScaling)
                            {
                                pushStrength *= 10f - (distance / component.MaxPushDistance);
                            }
                            var impulse = pushDirection * pushStrength;
                            _physics.ApplyLinearImpulse(entity, impulse, body: physicsComponent);
                        }
                    }
                }
                component.IsDamageAttack = false;
            }
        }
    }
}



