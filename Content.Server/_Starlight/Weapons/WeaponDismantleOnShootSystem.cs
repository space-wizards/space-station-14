using Content.Shared.Starlight.Weapon.Systems;
using System.Numerics;
using Content.Shared._Starlight.Weapon.Components;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Physics.Systems;

//linq
using System.Linq;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Random;
using Content.Shared.Damage;
using Content.Shared.Wieldable;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;

namespace Content.Server._Starlight.Weapon.Systems;
public sealed partial class WeaponDismantleOnShootSystem : SharedWeaponDismantleOnShootSystem
{
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] protected readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] protected readonly DamageableSystem Damageable = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WeaponDismantleOnShootComponent, AmmoShotEvent>(OnGunShot);
    }

    private void OnGunShot(Entity<WeaponDismantleOnShootComponent> ent, ref AmmoShotEvent args)
    {
        if (DismantleCheck(ent, ref args) == false)
            return;

        //apply the damage to the shooter
        //get the shooters damageable component
        Damageable.TryChangeDamage(args.Shooter, ent.Comp.SelfDamage, origin:args.Shooter);

        //we need the user past this point
        if (!args.Shooter.HasValue)
            return;

        _audio.PlayPvs(ent.Comp.DismantleSound, args.Shooter.Value);

        //get the users transform
        var userPosition = Transform(args.Shooter.Value).Coordinates;

        if (!TryComp<GunComponent>(ent, out var gunComponent))
            return;
        
        var toCoordinates = gunComponent.ShootCoordinates;

        if (toCoordinates == null)
            return;

        //loop through all of the items
        var random = IoCManager.Resolve<IRobustRandom>();
        foreach (var item in ent.Comp.items)
        {
            for (var i = 0; i < item.Amount; i++)
            {
                //roll to see if we destroy the item or not
                if (!random.Prob(item.SpawnProbability))
                    continue;

                //get the item entity
                var itemEntity = Spawn(item.PrototypeId, userPosition);

                Vector2 direction = toCoordinates.Value.Position;
                //normalize it
                direction = Vector2.Normalize(direction);
                //multiply it by the distance
                direction *= ent.Comp.DismantleDistance;
                //rotate it by the angle
                direction = item.LaunchAngle.RotateVec(direction);

                //roll for random angle modifier
                double randomAngle = random.NextDouble(-item.AngleRandomness.Degrees, item.AngleRandomness.Degrees);
                //rotate it by the random angle
                direction = Angle.FromDegrees(randomAngle).RotateVec(direction);

                var throwDirection = new EntityCoordinates(args.Shooter.Value, direction);

                _throwing.TryThrow(itemEntity, throwDirection, ent.Comp.DismantleDistance, compensateFriction: true);
            }
        }

        //now we need to destroy the gun
        //get the gun entity
        _entityManager.QueueDeleteEntity(ent.Owner);
    }
}
