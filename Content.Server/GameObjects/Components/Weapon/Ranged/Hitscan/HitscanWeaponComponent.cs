﻿using Content.Shared.GameObjects;
using SS14.Server.GameObjects.EntitySystems;
using SS14.Shared.Audio;
using SS14.Shared.GameObjects.EntitySystemMessages;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Physics;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Shared.Physics;
using SS14.Shared.Serialization;
using System;
 using Content.Server.GameObjects.Components.Sound;
 using SS14.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Hitscan
{
    public class HitscanWeaponComponent : Component
    {
        private const float MaxLength = 20;
        public override string Name => "HitscanWeapon";

        string _spritename;
        private int _damage;
        private int _internalCapacity;
        private int _currentInternalCharge; //this functionality should probably be delegated to a magazine component
        private int _baseFireCost;

        public int Damage => _damage;
        public int InternalCapacity => _internalCapacity;
        public int CurrentInternalCharge => _currentInternalCharge;
        public int BaseFireCost => _baseFireCost;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _spritename, "sprite", "Objects/laser.png");
            serializer.DataField(ref _damage, "damage", 10);
            serializer.DataField(ref _internalCapacity, "internalCapacity", 1500);
            serializer.DataField(ref _baseFireCost, "baseFireCost", 300);
        }

        public override void Initialize()
        {
            base.Initialize();

            _currentInternalCharge = _internalCapacity;
            var rangedWeapon = Owner.GetComponent<RangedWeaponComponent>();
            rangedWeapon.FireHandler = Fire;
        }

        private void Fire(IEntity user, GridCoordinates clickLocation)
        {
            double energyModifier = 1;
            if (_currentInternalCharge == 0)
            {
                return;
            }
            else if(_currentInternalCharge < _baseFireCost)
            {
                energyModifier = (double)_currentInternalCharge / (double)_baseFireCost;
                _currentInternalCharge = 0;
            }
            else
            {
                _currentInternalCharge = _currentInternalCharge - _baseFireCost;
            }
            var userPosition = user.Transform.WorldPosition; //Remember world positions are ephemeral and can only be used instantaneously
            var angle = new Angle(clickLocation.Position - userPosition);

            var ray = new Ray(userPosition, angle.ToVec());
            var rayCastResults = IoCManager.Resolve<IPhysicsManager>().IntersectRay(ray, MaxLength,
                Owner.Transform.GetMapTransform().Owner);

            Hit(rayCastResults, energyModifier);
            AfterEffects(user, rayCastResults, angle, energyModifier);
        }

        protected virtual void Hit(RayCastResults ray, double damageModifier)
        {
            if (ray.HitEntity != null && ray.HitEntity.TryGetComponent(out DamageableComponent damage))
            {
                damage.TakeDamage(DamageType.Heat, (int)Math.Round(_damage * damageModifier, MidpointRounding.AwayFromZero));
                //I used Math.Round over Convert.toInt32, as toInt32 always rounds to
                //even numbers if halfway between two numbers, rather than rounding to nearest
            }
        }

        protected virtual void AfterEffects(IEntity user, RayCastResults ray, Angle angle, double energyModifier)
        {
            var time = IoCManager.Resolve<IGameTiming>().CurTime;
            var dist = ray.DidHitObject ? ray.Distance : MaxLength;
            var offset = angle.ToVec() * dist / 2;
            var message = new EffectSystemMessage
            {
                EffectSprite = _spritename,
                Born = time,
                DeathTime = time + TimeSpan.FromSeconds(1),
                Size = new Vector2(dist, 1f),
                Coordinates = user.Transform.GridPosition.Translated(offset),
                //Rotated from east facing
                Rotation = (float) angle.Theta,
                ColorDelta = new Vector4(0, 0, 0, -1500f),
                Color = Vector4.Multiply(new Vector4(255, 255, 255, 750), (float)energyModifier),

                Shaded = false
            };
            var mgr = IoCManager.Resolve<IEntitySystemManager>();
            mgr.GetEntitySystem<EffectSystem>().CreateParticle(message);
            Owner.GetComponent<SoundComponent>().Play("/Audio/laser.ogg", AudioParams.Default.WithVolume(-5));
        }
    }
}
