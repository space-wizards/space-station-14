using Content.Server.Weapon.Ranged.Systems;
using Content.Shared.Item;
using Content.Shared.Projectiles;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Systems;


namespace Content.Server.Baseball
{
    public sealed class BaseballBatSystem : EntitySystem
    {
        [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
        [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly GunSystem _gunSystem = default!;

        /// <inheritdoc/>
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BaseballBatComponent, WideAttackEvent>(OnBatAttack);
        }

        public void OnBatAttack(EntityUid uid, BaseballBatComponent component, WideAttackEvent args)
        {

            var location = EntityManager.GetComponent<TransformComponent>(args.User).Coordinates;
            var diff = args.ClickLocation.ToMapPos(EntityManager) - location.ToMapPos(EntityManager);


            //if (!EntityManager.TryGetComponent<ThrownItemComponent>(args.User, out var thrownItemComponent))
            //    return;

            //EntityManager.RemoveComponent<ThrownItemComponent>(args.User);

            var dir = args.ClickLocation.ToMapPos(EntityManager) - EntityManager.GetComponent<TransformComponent>(args.User).WorldPosition.Normalized;

            //The melee system uses a collision raycast but apparently that doesnt work with items so GetEntitiesInArc it is!
            foreach (var entity in _entityLookupSystem.GetEntitiesInArc(location, 0.5f, diff.ToAngle(), 50f, LookupFlags.None))
            {
                //if (EntityManager.HasComponent<ItemComponent>(entity) && EntityManager.HasComponent<ThrownItemComponent>(entity))
                if (EntityManager.HasComponent<ItemComponent>(entity))
                {

                    var rand = _random.Next(1, 5); // random chance of fireball wack

                    if (rand < 4)
                    {
                        _throwingSystem.TryThrow(entity, diff + _random.NextAngle(10f, 180f).ToVec(), 10f * _random.NextFloat(2, component.WackForceMultiplier), uid);
                        if (EntityManager.HasComponent<ThrownItemComponent>(entity))
                            _audioSystem.Play("/Audio/Effects/hit_kick.ogg", Filter.Pvs(args.User), args.User, AudioParams.Default);
                        return;
                    }

                    //just shoots a wizard fireball
                    var fireball = Spawn("ProjectileFireball", EntityManager.GetComponent<TransformComponent>(entity).Coordinates);
                    EntityManager.DeleteEntity(entity);
                    _gunSystem.ShootProjectile(fireball, diff, args.User);
                    _audioSystem.Play("/Audio/Effects/hit_kick.ogg", Filter.Pvs(args.User), args.User, AudioParams.Default);
                }
            }
        }
    }
}
