using System.Numerics;
using Content.Shared.CosmicCult;
using Content.Shared.CosmicCult.Components;
using Content.Shared.CosmicCult.Components.Actions;
using Content.Shared.Damage.Systems;
using Content.Shared.Effects;
using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.CosmicCult.Abilities;

public sealed partial class CosmicNovaSystem : EntitySystem
{
    [Dependency] private CosmicCultSystem _cosmicCult = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedColorFlashEffectSystem _color = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedProjectileSystem _projectile = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    /// <summary>
    /// This is the basic spell projectile code but updated to use non-obsolete functions, all so i can change the default projectile speed. Fuck.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnCosmicNova(Entity<CosmicActionNovaComponent> ent, ref EventCosmicNova args)
    {
        if (!TryComp<CosmicCultActionComponent>(ent, out var action))
            return;

        var startPos = _transform.GetMapCoordinates(args.Performer);
        var targetPos = _transform.ToMapCoordinates(args.Target);
        var userVelocity = _physics.GetMapLinearVelocity(args.Performer);

        var delta = targetPos.Position - startPos.Position;
        if (delta.EqualsApprox(Vector2.Zero))
            delta = new(.01f, 0);

        args.Handled = true;
        var proj = Spawn(ent.Comp.Projectile, startPos);
        _gun.ShootProjectile(proj, delta, userVelocity, args.Performer, args.Performer, ent.Comp.ProjectileSpeed);
        _audio.PlayPvs(action.Sfx, ent, AudioParams.Default.WithVariation(0.1f));
    }

    [SubscribeLocalEvent]
    private void OnNovaCollide(Entity<CosmicNovaComponent> uid, ref StartCollideEvent args)
    {
        if (_cosmicCult.EntityIsCultist(args.OtherEntity) || !HasComp<MobStateComponent>(args.OtherEntity))
            return;
        if (uid.Comp.DoStun)
            _stun.TryUpdateParalyzeDuration(args.OtherEntity, TimeSpan.FromSeconds(1f));
        _damageable.TryChangeDamage(args.OtherEntity, uid.Comp.CosmicNovaDamage); // This can possibly trigger two or three times because of how collision works. Keep that in mind.
        _color.RaiseEffect(Color.Red, new List<EntityUid>() { args.OtherEntity }, Filter.Pvs(args.OtherEntity, entityManager: EntityManager));
    }
}
