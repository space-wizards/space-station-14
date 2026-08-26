// SPDX-FileCopyrightText: 2025 AftrLite
// SPDX-FileCopyrightText: 2025 Janet Blackquill <uhhadd@gmail.com>
//
// SPDX-License-Identifier: LicenseRef-CosmicCult

using System.Numerics;
using Content.Shared.CosmicCult;
using Content.Shared.CosmicCult.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Effects;
using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;
using Content.Shared.Stunnable;
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
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedColorFlashEffectSystem _color = default!;
    [Dependency] private SharedCosmicCultSystem _cosmicCult = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedProjectileSystem _projectile = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly EntProtoId Projectile = "ProjectileCosmicNova";

    /// <summary>
    /// This is the basic spell projectile code but updated to use non-obsolete functions, all so i can change the default projectile speed. Fuck.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnCosmicNova(Entity<CosmicCultistComponent> uid, ref EventCosmicNova args)
    {
        var startPos = _transform.GetMapCoordinates(args.Performer);
        var targetPos = _transform.ToMapCoordinates(args.Target);
        var userVelocity = _physics.GetMapLinearVelocity(args.Performer);

        var delta = targetPos.Position - startPos.Position;
        if (delta.EqualsApprox(Vector2.Zero))
            delta = new(.01f, 0);

        args.Handled = true;
        var ent = Spawn(Projectile, startPos);
        ShootProjectile(ent, delta, userVelocity, args.Performer, args.Performer, 5f);
        _audio.PlayPvs(uid.Comp.NovaCastSfx, uid, AudioParams.Default.WithVariation(0.1f));
    }

    // AftrLite, why aren't you using _gunSystem's ShootProjectile()? | Because i've decoupled a lot of the GunSystem on Stellar and don't want to rely on it.
    // TODO: Move this into Stellar's Gun System.
    private void ShootProjectile(EntityUid uid, Vector2 direction, Vector2 gunVelocity, EntityUid? gunUid, EntityUid? user = null, float speed = 10f)
    {
        var physics = EnsureComp<PhysicsComponent>(uid);
        var projectile = EnsureComp<ProjectileComponent>(uid);

        var targetMapVelocity = gunVelocity + direction.Normalized() * speed;
        var currentMapVelocity = _physics.GetMapLinearVelocity(uid, physics);
        var finalLinear = physics.LinearVelocity + targetMapVelocity - currentMapVelocity;
        _physics.SetLinearVelocity(uid, finalLinear, body: physics);
        _physics.SetBodyStatus(uid, physics, BodyStatus.InAir);

        projectile.Weapon = gunUid;
        var shooter = user ?? gunUid;
        if (shooter != null)
            _projectile.SetShooter(uid, projectile, shooter.Value);

        _transform.SetWorldRotation(uid, direction.ToWorldAngle() + projectile.Angle);
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
