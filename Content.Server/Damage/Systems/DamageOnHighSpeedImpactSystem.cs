using Content.Server.Damage.Components;
using Content.Server.Stunnable;
using Content.Shared.Damage;
using Content.Shared.Effects;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Damage.Systems;

public sealed class DamageOnHighSpeedImpactSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly StunSystem _stun = default!;

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

        if ((_gameTiming.CurTime - component.LastHit).TotalSeconds < component.DamageCooldown)
            return;

        component.LastHit = _gameTiming.CurTime;

        if (_robustRandom.Prob(component.StunChance))
            _stun.TryStun(uid, TimeSpan.FromSeconds(component.StunSeconds), true);

        var damageScale = component.SpeedDamageFactor * speed / component.MinimumSpeed;

        _damageable.TryChangeDamage(uid, component.Damage * damageScale);

        _audio.PlayPvs(component.SoundHit, uid, AudioParams.Default.WithVariation(0.125f).WithVolume(-0.125f));
        _color.RaiseEffect(Color.Red, new List<EntityUid>() { uid }, Filter.Pvs(uid, entityManager: EntityManager));
    }

    public void ChangeCollide(EntityUid uid, float minimumSpeed, float stunSeconds, float damageCooldown, DamageOnHighSpeedImpactComponent? collide = null)
    {
        if (!Resolve(uid, ref collide, false))
            return;

        collide.MinimumSpeed = minimumSpeed;
        collide.StunSeconds = stunSeconds;
        collide.DamageCooldown = damageCooldown;
        Dirty(uid, collide);
    }
}
