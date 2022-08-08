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

            if (!TryComp<MeleeWeaponComponent>(component.Owner, out var meleeWeaponComponent))
                return;
            if (_gameTiming.CurTime < meleeWeaponComponent.CooldownEnd)
                return;

            var location = Comp<TransformComponent>(args.User).Coordinates;
            var dir = args.ClickLocation.ToMapPos(EntityManager) - location.ToMapPos(EntityManager);
            var dirForceMultiplier = _random.NextFloat(component.WackForceMultiplierMin, component.WackForceMultiplierMax);

            var hitStrength = _random.NextFloat(component.WackStrengthMin, component.WackStrengthMax);

            var physicsQuery = GetEntityQuery<PhysicsComponent>();
            var thrownItemQuery = GetEntityQuery<ThrownItemComponent>();

            //The melee system uses a collision raycast but apparently that doesnt work with items so im using GetEntitiesInArc
            foreach (var entity in _entityLookupSystem.GetEntitiesInArc(location, meleeWeaponComponent.Range, dir.ToAngle(), meleeWeaponComponent.ArcWidth))
            {
                //Checking to see if the items are actually thrown
                if (!physicsQuery.TryGetComponent(entity, out var physicsComponent))
                    continue;
                if (!thrownItemQuery.HasComponent(entity))
                    continue;

                var rand = _random.Next(1, component.FireballChance + 1); // Rolling to see if we'll get the fireball

                if (rand != component.FireballChance)
                {
                    physicsComponent.Momentum = Vector2.Zero; //stopping the item so we get a clean throw
                    _throwingSystem.TryThrow(entity, dir * dirForceMultiplier, hitStrength, uid);
                    _audioSystem.Play(component.HitSound, Filter.Pvs(args.User), args.User, AudioParams.Default);
                    return;
                }

                //just shoots a wizard fireball
                var fireball = Spawn("ProjectileFireball", Comp<TransformComponent>(entity).Coordinates);
                Del(entity);
                _gunSystem.ShootProjectile(fireball, dir, args.User);
                _audioSystem.Play("/Audio/Effects/baseball-hit-extreme.ogg", Filter.Pvs(args.User), args.User, AudioParams.Default);
            }
        }
    }
}
