// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Movement.Systems;
using Content.Shared.DeadSpace.Demons.Herald.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Content.Shared.Weapons.Melee;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs;

namespace Content.Shared.DeadSpace.Demons.Herald.EntitySystems;

public abstract class SharedHeraldSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HeraldComponent, RefreshMovementSpeedModifiersEvent>(OnRefresh);
        SubscribeLocalEvent<HeraldComponent, MobStateChangedEvent>(OnDead);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HeraldComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {

            if (_gameTiming.CurTime > comp.EnrageTime && comp.isEnrage)
            {
                StopEnrage(uid, comp);
            }

            if (_gameTiming.CurTime > comp.TimeUtilDead && comp.IsDead)
            {
                QueueDel(uid);
            }
        }
    }

    private void OnRefresh(EntityUid uid, HeraldComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.MovementSpeedBuff, component.MovementSpeedBuff);
    }

    private void OnDead(EntityUid uid, HeraldComponent component, MobStateChangedEvent args)
    {
        if (_mobState.IsDead(uid))
        {
            _audio.PlayPvs(component.SoundRoar, uid);
            component.IsDead = true;
            component.TimeUtilDead = _gameTiming.CurTime + component.DeadDuration;
        }
    }

    public virtual void StartEnrage(EntityUid uid, HeraldComponent component)
    {
        _appearance.SetData(uid, HeraldVisuals.Enraged, true);
        _audio.PlayPvs(component.SoundRoar, uid);

        component.EnrageTime = _gameTiming.CurTime + component.EnrageDuration;
        component.isEnrage = true;
        component.MovementSpeedBuff = 3f;
        _movement.RefreshMovementSpeedModifiers(uid);
        if (!EntityManager.TryGetComponent(uid, out MeleeWeaponComponent? weapon))
            return;

        weapon.Damage = weapon.Damage * component.DamageModifier;
    }

    public virtual void StopEnrage(EntityUid uid, HeraldComponent component)
    {
        _appearance.SetData(uid, HeraldVisuals.Enraged, false);
        component.isEnrage = false;

        component.MovementSpeedBuff = 1.5f;
        _movement.RefreshMovementSpeedModifiers(uid);
        if (!EntityManager.TryGetComponent(uid, out MeleeWeaponComponent? weapon))
            return;

        weapon.Damage = weapon.Damage / component.DamageModifier;
    }
}
