using System.Numerics;
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
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.CosmicCult.Abilities;

public sealed partial class CosmicNovaSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;

    [Dependency] private CosmicCultSystem _cult = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedColorFlashEffectSystem _color = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    /// <summary>
    /// This is the basic spell projectile code but updated to use non-obsolete functions, all so i can change the default projectile speed. Fuck.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnCosmicNova(Entity<CosmicActionNovaComponent> ent, ref EventCosmicNova args)
    {
        if (!_cult.CultActionQuery.TryComp(ent, out var action) || args.Handled)
            return;

        args.Handled = true;

        var startPos = _transform.GetMapCoordinates(args.Performer);
        var targetPos = _transform.ToMapCoordinates(args.Target);
        var userVelocity = _physics.GetMapLinearVelocity(args.Performer) * 0.4f;

        var delta = targetPos.Position - startPos.Position;
        if (delta.EqualsApprox(Vector2.Zero))
            delta = new(.01f, 0);

        if (_net.IsClient && _timing.IsFirstTimePredicted)
            SpawnAttachedTo(ent.Comp.IndicatorEffect, Transform(ent).Coordinates);

        if (_net.IsServer) // Unpredicted projectiles make me sad, but it's the only way to do this right now. If we move the spawn or audio outside IsServer, it'll be very noticeably desynced and that's worse than just having the whole thing unpredicted.
        {
            _audio.PlayPvs(action.Sfx, args.Performer, AudioParams.Default.WithVariation(0.1f));
            _gun.ShootProjectile(PredictedSpawn(ent.Comp.Projectile, startPos), delta, userVelocity, args.Performer, args.Performer, ent.Comp.ProjectileSpeed);
        }
    }

    [SubscribeLocalEvent]
    private void OnNovaCollide(Entity<CosmicNovaComponent> uid, ref StartCollideEvent args)
    {
        if (_cult.EntityIsCultist(args.OtherEntity) || !HasComp<MobStateComponent>(args.OtherEntity))
            return;
        if (uid.Comp.DoStun)
            _stun.TryUpdateParalyzeDuration(args.OtherEntity, TimeSpan.FromSeconds(1f));
        _damageable.TryChangeDamage(args.OtherEntity, uid.Comp.CosmicNovaDamage); // This can possibly trigger two or three times because of how collision works. Keep that in mind.
        _color.RaiseEffect(Color.Red, new List<EntityUid>() { args.OtherEntity }, Filter.Pvs(args.OtherEntity, entityManager: EntityManager));
    }
}
