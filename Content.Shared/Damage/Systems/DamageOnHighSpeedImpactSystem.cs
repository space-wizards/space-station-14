using Content.Shared.Stunnable;
using Content.Shared.Damage.Components;
using Content.Shared.Effects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Damage.Systems;

public sealed class DamageOnHighSpeedImpactSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DamageOnHighSpeedImpactComponent, StartCollideEvent>(HandleCollide);
    }

    private void HandleCollide(EntityUid uid, DamageOnHighSpeedImpactComponent component, ref StartCollideEvent args)
    {
        if (!args.OurFixture.Hard || !args.OtherFixture.Hard)
            return;

        if (!EntityManager.HasComponent<DamageableComponent>(uid))
            return;

        var speed = args.OurBody.LinearVelocity.Length();

        if (speed < component.MinimumSpeed)
            return;

        if (component.LastHit != null
            && (_gameTiming.CurTime - component.LastHit.Value).TotalSeconds < component.DamageCooldown)
            return;

        component.LastHit = _gameTiming.CurTime;

        if (_robustRandom.Prob(component.StunChance))
            _stun.TryStun(uid, TimeSpan.FromSeconds(component.StunSeconds), true);

        var damageScale = component.SpeedDamageFactor * speed / component.MinimumSpeed;

        _damageable.TryChangeDamage(uid, component.Damage * damageScale);

        if (_gameTiming.IsFirstTimePredicted)
            _audio.PlayPvs(component.SoundHit, uid, AudioParams.Default.WithVariation(0.125f).WithVolume(-0.125f));
        _color.RaiseEffect(Color.Red, new List<EntityUid>() { uid }, Filter.Pvs(uid, entityManager: EntityManager));
    }

    public void ChangeCollide(EntityUid uid, float minimumSpeed, float stunSeconds, float damageCooldown, float speedDamage, DamageOnHighSpeedImpactComponent? collide = null)
    {
        if (!Resolve(uid, ref collide, false))
            return;

        collide.MinimumSpeed = minimumSpeed;
        collide.StunSeconds = stunSeconds;
        collide.DamageCooldown = damageCooldown;
        collide.SpeedDamageFactor = speedDamage;
        Dirty(uid, collide);
    }
}
