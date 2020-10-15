#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Projectiles;
using Content.Shared.GameObjects.Components.Weapons.Ranged;
using Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Physics;
using JetBrains.Annotations;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class RangedWeaponSystem : SharedRangedWeaponSystem
    {
        // Handles fire positions etc. from clients
        // It'd be cleaner to have this under corresponding Client / Server components buuuttt the issue with that is
        // you wouldn't be able to inherit from "SharedBlankWeapon" and would instead need to make
        // discrete server and client versions of each weapon that don't inherit from shared.

        // e.g. SharedRangedWeapon -> ServerRevolver and SharedRangedWeapon -> ClientRevolver
        // (Handles syncing via component)
        // vs.
        // SharedRangedWeapon -> SharedRevolver -> ServerRevolver
        // (needs to sync via system or component spaghetti)

        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPhysicsManager _physicsManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private EffectSystem _effectSystem = default!;

        private List<SharedRangedWeaponComponent> _activeRangedWeapons = new List<SharedRangedWeaponComponent>();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<StartFiringMessage>(HandleStartMessage);
            SubscribeNetworkEvent<StopFiringMessage>(HandleStopMessage);
            SubscribeNetworkEvent<RangedFireMessage>(HandleRangedFireMessage);

            _effectSystem = Get<EffectSystem>();
        }

        private void HandleStartMessage(StartFiringMessage message, EntitySessionEventArgs args)
        {
            var entity = _entityManager.GetEntity(message.Uid);
            var weapon = entity.GetComponent<SharedRangedWeaponComponent>();

            if (entity.Deleted)
            {
                _activeRangedWeapons.Remove(weapon);
                return;
            }

            var shooter = weapon.Shooter();
            if (shooter != args.SenderSession.AttachedEntity)
            {
                // Cheater / lagger
                return;
            }

            if (!_activeRangedWeapons.Contains(weapon))
            {
                _activeRangedWeapons.Add(weapon);
            }

            weapon.ShotCounter = 0;
            weapon.FireCoordinates = message.FireCoordinates;
            weapon.Firing = true;
        }

        private void HandleStopMessage(StopFiringMessage message, EntitySessionEventArgs args)
        {
            var entity = _entityManager.GetEntity(message.Uid);
            var weapon = entity.GetComponent<SharedRangedWeaponComponent>();

            if (entity.Deleted)
            {
                _activeRangedWeapons.Remove(weapon);
                return;
            }

            var shooter = weapon.Shooter();
            if (shooter != args.SenderSession.AttachedEntity)
            {
                // Cheater / lagger
                return;
            }

            weapon.Firing = false;
            weapon.ExpectedShots = message.ExpectedShots;
        }

        private void HandleRangedFireMessage(RangedFireMessage message, EntitySessionEventArgs args)
        {
            var entity = _entityManager.GetEntity(message.Uid);
            var weapon = entity.GetComponent<SharedRangedWeaponComponent>();

            if (entity.Deleted)
            {
                _activeRangedWeapons.Remove(weapon);
                return;
            }

            var shooter = weapon.Shooter();
            if (shooter != args.SenderSession.AttachedEntity)
            {
                // Cheater / lagger
                return;
            }

            weapon.FireCoordinates = message.FireCoordinates;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            var currentTime = _gameTiming.CurTime;

            for (var i = _activeRangedWeapons.Count - 1; i >= 0; i--)
            {
                var comp = _activeRangedWeapons[i];

                if (!TryUpdate(comp, currentTime))
                {
                    _activeRangedWeapons.RemoveAt(i);
                    comp.ExpectedShots = 0;
                    comp.AccumulatedShots = 0;
                    comp.Dirty();
                }
            }
        }

        private bool TryUpdate(SharedRangedWeaponComponent weaponComponent, TimeSpan currentTime)
        {
            if (weaponComponent.FireCoordinates == null || !weaponComponent.Firing && weaponComponent.AccumulatedShots >= weaponComponent.ExpectedShots)
            {
                if (weaponComponent.AccumulatedShots != weaponComponent.ExpectedShots)
                {
                    Logger.Warning($"Shooting desync occurred: Fired {weaponComponent.ShotCounter} but expected {weaponComponent.ExpectedShots}");
                }

                weaponComponent.ExpectedShots -= weaponComponent.AccumulatedShots;
                return false;
            }

            var shooter = weaponComponent.Shooter();
            if (shooter == null)
                return false;

            if (!weaponComponent.TryFire(currentTime, shooter, weaponComponent.FireCoordinates.Value, out var shots))
                return false;

            weaponComponent.AccumulatedShots += shots;
            return true;
        }

        public override void ShootHitscan(IEntity? user, SharedRangedWeaponComponent weapon, HitscanPrototype hitscan, Angle angle, float damageRatio = 1, float alphaRatio = 1)
        {
            var currentTime = _gameTiming.CurTime;
            var ray = new CollisionRay(weapon.Owner.Transform.MapPosition.Position, angle.ToVec(), (int) hitscan.CollisionMask);
            var rayCastResults = _physicsManager.IntersectRay(weapon.Owner.Transform.MapID, ray, hitscan.MaxLength, user, false).ToArray();
            float distance = hitscan.MaxLength;

            if (rayCastResults.Length >= 1)
            {
                var result = rayCastResults[0];
                distance = result.HitEntity != null ? result.Distance : hitscan.MaxLength;

                if (result.HitEntity == null || !result.HitEntity.TryGetComponent(out IDamageableComponent? damageable))
                    return;

                damageable.ChangeDamage(hitscan.DamageType, (int) Math.Round(hitscan.Damage, MidpointRounding.AwayFromZero), false, user);
                //I used Math.Round over Convert.toInt32, as toInt32 always rounds to
                //even numbers if halfway between two numbers, rather than rounding to nearest
            }

            // Fire effects
            HitscanMuzzleFlash(user, weapon, hitscan.MuzzleEffect, angle, distance, currentTime, alphaRatio);
            TravelFlash(user, weapon.Owner, hitscan, angle, distance, currentTime, alphaRatio);
            ImpactFlash(user, weapon.Owner, hitscan, angle, distance, currentTime, alphaRatio);
        }

        public override void ShootAmmo(IEntity? user, SharedRangedWeaponComponent weapon, Angle angle, SharedAmmoComponent ammoComponent)
        {
            if (!ammoComponent.CanFire())
                return;

            List<Angle>? sprayAngleChange = null;
            var count = ammoComponent.ProjectilesFired;
            var evenSpreadAngle = ammoComponent.EvenSpreadAngle;
            var spreadRatio = weapon.AmmoSpreadRatio;

            if (ammoComponent.AmmoIsProjectile)
            {
                ShootProjectile(user, weapon, angle, ammoComponent.Owner.GetComponent<SharedProjectileComponent>(), ammoComponent.Velocity);
                return;
            }

            if (count > 1)
            {
                evenSpreadAngle *= spreadRatio;
                sprayAngleChange = Linspace(-evenSpreadAngle / 2, evenSpreadAngle / 2, count);
            }

            for (var i = 0; i < count; i++)
            {
                var projectile =
                        EntityManager.SpawnEntity(ammoComponent.ProjectileId, ammoComponent.Owner.Transform.MapPosition);

                Angle projectileAngle;

                if (sprayAngleChange != null)
                {
                    projectileAngle = angle + sprayAngleChange[i];
                }
                else
                {
                    projectileAngle = angle;
                }

                if (_prototypeManager.TryIndex(ammoComponent.ProjectileId, out HitscanPrototype hitscan))
                {
                    ShootHitscan(user, weapon, hitscan, angle);
                }
                else
                {
                    ShootProjectile(user, weapon, projectileAngle, projectile.GetComponent<SharedProjectileComponent>(), ammoComponent.Velocity);
                }
            }
        }

        public override void ShootProjectile(IEntity? user, SharedRangedWeaponComponent weapon, Angle angle, SharedProjectileComponent projectileComponent, float velocity)
        {
            var physicsComponent = projectileComponent.Owner.GetComponent<IPhysicsComponent>();
            physicsComponent.Status = BodyStatus.InAir;

            if (user != null)
                projectileComponent.IgnoreEntity(user);

            physicsComponent
                .EnsureController<BulletController>()
                .LinearVelocity = angle.ToVec() * velocity;

            projectileComponent.Owner.Transform.LocalRotation = angle.Theta;
        }

        private List<Angle> Linspace(double start, double end, int intervals)
        {
            var linspace = new List<Angle>(intervals);

            for (var i = 0; i <= intervals - 1; i++)
            {
                linspace.Add(Angle.FromDegrees(start + (end - start) * i / (intervals - 1)));
            }
            return linspace;
        }

        public override void MuzzleFlash(IEntity? user, SharedRangedWeaponComponent weapon, Angle angle, TimeSpan? currentTime = null, bool predicted = true, float alphaRatio = 1.0f)
        {
            var texture = weapon.MuzzleFlash;
            if (texture == null)
                return;

            currentTime ??= _gameTiming.CurTime;
            var offset = angle.ToVec().Normalized / 2;
            var message = new EffectSystemMessage
            {
                EffectSprite = texture,
                Born = currentTime.Value,
                DeathTime = _gameTiming.CurTime + TimeSpan.FromSeconds(EffectDuration),
                AttachedEntityUid = weapon.Owner.Uid,
                AttachedOffset = offset,
                //Rotated from east facing
                Rotation = (float) angle.Theta,
                Color = Vector4.Multiply(new Vector4(255, 255, 255, 750), alphaRatio),
                ColorDelta = new Vector4(0, 0, 0, -1500f),
                Shaded = false
            };

            _effectSystem.CreateParticle(message, predicted ? user?.PlayerSession() : null);
        }

        private void HitscanMuzzleFlash(IEntity? user, SharedRangedWeaponComponent weapon, string? texture, Angle angle, float distance, TimeSpan? currentTime = null, float alphaRatio = 1.0f)
        {
            if (texture == null || distance <= 1.0f)
                return;

            currentTime ??= _gameTiming.CurTime;
            var parent = user ?? weapon.Owner;

            var message = new EffectSystemMessage
            {
                EffectSprite = texture,
                Born = currentTime.Value,
                DeathTime = _gameTiming.CurTime + TimeSpan.FromSeconds(EffectDuration),
                Coordinates = parent.Transform.Coordinates.Offset(angle.ToVec().Normalized * 0.5f),
                //Rotated from east facing
                Rotation = (float) angle.Theta,
                Color = Vector4.Multiply(new Vector4(255, 255, 255, 750), alphaRatio),
                ColorDelta = new Vector4(0, 0, 0, -1500f),
                Shaded = false
            };

            _effectSystem.CreateParticle(message);
        }

        private void TravelFlash(IEntity? user, IEntity weapon, HitscanPrototype hitscan, Angle angle, float distance, TimeSpan? currentTime = null, float alphaRatio = 1.0f)
        {
            if (hitscan.TravelEffect == null || distance <= 1.5f)
                return;

            currentTime ??= _gameTiming.CurTime;
            var parent = user ?? weapon;
            const float offset = 0.5f;

            var message = new EffectSystemMessage
            {
                EffectSprite = hitscan.TravelEffect,
                Born = _gameTiming.CurTime,
                DeathTime = currentTime.Value + TimeSpan.FromSeconds(EffectDuration),
                Size = new Vector2(distance - offset , 1f),
                Coordinates = parent.Transform.Coordinates.Offset(angle.ToVec() * (distance + offset) / 2),
                //Rotated from east facing
                Rotation = (float) angle.FlipPositive(),
                Color = Vector4.Multiply(new Vector4(255, 255, 255, 750), alphaRatio),
                ColorDelta = new Vector4(0, 0, 0, -1500f),
                Shaded = false
            };

            _effectSystem.CreateParticle(message);
        }

        private void ImpactFlash(IEntity? user, IEntity weapon, HitscanPrototype hitscan, Angle angle, float distance, TimeSpan? currentTime = null, float alphaRatio = 1.0f)
        {
            if (hitscan.ImpactEffect == null)
                return;

            currentTime ??= _gameTiming.CurTime;
            var parent = user ?? weapon;

            var message = new EffectSystemMessage
            {
                EffectSprite = hitscan.ImpactEffect,
                Born = _gameTiming.CurTime,
                DeathTime = currentTime.Value + TimeSpan.FromSeconds(EffectDuration),
                Coordinates = parent.Transform.Coordinates.Offset(angle.ToVec().Normalized * distance),
                //Rotated from east facing
                Rotation = (float) angle.FlipPositive(),
                Color = Vector4.Multiply(new Vector4(255, 255, 255, 750), alphaRatio),
                ColorDelta = new Vector4(0, 0, 0, -1500f),
                Shaded = false
            };

            _effectSystem.CreateParticle(message);
        }

        public override void EjectCasing(IEntity? user, IEntity casing, bool playSound = true, Direction[]? ejectDirections = null)
        {
            ejectDirections ??= new[] {Direction.East, Direction.North, Direction.South, Direction.West};

            const float ejectOffset = 0.2f;

            var ammo = casing.GetComponent<SharedAmmoComponent>();
            var offsetPos = (_robustRandom.NextFloat() * ejectOffset, _robustRandom.NextFloat() * ejectOffset);

            // Need to deparent it if applicable
            if (user != null && casing.Transform.ParentUid == user.Uid && user.Transform.Parent != null)
            {
                casing.Transform.Coordinates = user.Transform.Coordinates.Offset(offsetPos);
            }
            else
            {
                casing.Transform.Coordinates = casing.Transform.Coordinates.Offset(offsetPos);
            }

            casing.Transform.LocalRotation = _robustRandom.Pick(ejectDirections).ToAngle();

            if (ammo.SoundCollectionEject == null || !playSound)
                return;

            var soundCollection = _prototypeManager.Index<SoundCollectionPrototype>(ammo.SoundCollectionEject);
            var randomFile = _robustRandom.Pick(soundCollection.PickFiles);
            // Don't use excluded til cartridges predicted

            Get<AudioSystem>().PlayFromEntity(randomFile, casing, AudioHelpers.WithVariation(0.2f, _robustRandom).WithVolume(-1));
        }
    }
}
