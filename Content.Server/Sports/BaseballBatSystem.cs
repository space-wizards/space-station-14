using Content.Server.Sports.Components;
using Content.Server.Weapon.Melee.Components;
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
using Robust.Shared.Timing;


namespace Content.Server.Sports
{
    public sealed class BaseballBatSystem : EntitySystem
    {
        [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
        [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly GunSystem _gunSystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BaseballBatComponent, WideAttackEvent>(OnBatAttack);
        }

        public void OnBatAttack(EntityUid uid, BaseballBatComponent component, WideAttackEvent args)
        {
            args.Handled = true;

            if (!EntityManager.TryGetComponent<MeleeWeaponComponent>(component.Owner, out var meleeWeaponComponent))
                return;
            if (_gameTiming.CurTime < meleeWeaponComponent.CooldownEnd)
                return;

            var location = EntityManager.GetComponent<TransformComponent>(args.User).Coordinates;
            var dir = args.ClickLocation.ToMapPos(EntityManager) - location.ToMapPos(EntityManager);

            var dirForceMultiplier = _random.NextFloat(component.WackForceMultiplierMin, component.WackForceMultiplierMax);

            var hitStrength = _random.NextFloat(component.WackStrengthMin, component.WackStrengthMax);

            //The melee system uses a collision raycast but apparently that doesnt work with items so im using GetEntitiesInArc
            foreach (var entity in _entityLookupSystem.GetEntitiesInArc(location, meleeWeaponComponent.Range, dir.ToAngle(), meleeWeaponComponent.ArcWidth))
            {
                //Checking to see if the items are actually throwable
                if (!EntityManager.HasComponent<ItemComponent>(entity))
                    return;
                if (EntityManager.TryGetComponent<TransformComponent>(entity, out var transformComponent) && transformComponent.Anchored)
                    return;

                if (!EntityManager.TryGetComponent<PhysicsComponent>(entity, out var physicsComponent))
                    return;

                if (EntityManager.HasComponent<ThrownItemComponent>(entity) || !component.OnlyHitThrown)
                {

                    var rand = _random.Next(1, component.FireballChance + 1); // Rolling to see if we'll get the fireball

                    if (rand != component.FireballChance)
                    {
                        physicsComponent.Momentum = Vector2.Zero; //stopping the item so we get a clean throw

                        _throwingSystem.TryThrow(entity, dir * dirForceMultiplier, hitStrength, uid);
                        if (hitStrength < 1) //More pathetic sound if you're hit is pathetic
                        {
                            _audioSystem.Play(component.BadHitSound, Filter.Pvs(args.User), args.User, AudioParams.Default);
                            return;
                        }
                        _audioSystem.Play(component.GoodHitSound, Filter.Pvs(args.User), args.User, AudioParams.Default);
                        return;
                    }

                    //just shoots a wizard fireball
                    var fireball = Spawn("ProjectileFireball", EntityManager.GetComponent<TransformComponent>(entity).Coordinates);
                    EntityManager.DeleteEntity(entity);
                    _gunSystem.ShootProjectile(fireball, dir, args.User);
                    _audioSystem.Play("/Audio/Effects/hit_kick.ogg", Filter.Pvs(args.User), args.User, AudioParams.Default);
                }
            }
        }
    }
}
