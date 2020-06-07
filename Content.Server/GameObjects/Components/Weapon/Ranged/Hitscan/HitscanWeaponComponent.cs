using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Power;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects;
using Content.Shared.Physics;
using Microsoft.EntityFrameworkCore.Internal;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;
#nullable enable

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Hitscan
{
    [RegisterComponent]
    public class HitscanWeaponComponent : Component, IInteractUsing
    {
#pragma warning disable CS8618 // Non-nullable field is uninitialized. 
        string _spritename;
        private int _damage;
        private int _baseFireCost;
        private float _lowerChargeLimit;
        private string _fireSound;
#pragma warning restore CS8618 // Non-nullable field is uninitialized.

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _spritename, "fireSprite", "Objects/laser.png");
            serializer.DataField(ref _damage, "damage", 10);
            serializer.DataField(ref _baseFireCost, "baseFireCost", 300);
            serializer.DataField(ref _lowerChargeLimit, "lowerChargeLimit", 10);
            serializer.DataField(ref _fireSound, "fireSound", "/Audio/laser.ogg");
        }

        //Remember to add it in both the .yaml prototype and the factory in EntryPoint.cs
        private HitscanWeaponCapacitorComponent _capacitorComponent;
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
        public override void Initialize()
        {
            base.Initialize();
            _capacitorComponent = Owner.GetComponent<HitscanWeaponCapacitorComponent>();
            Owner.GetComponent<RangedWeaponComponent>().FireHandler = Fire;
        }

        //Should this be hardcoded here? Shouldn't this be exposed to YAML with a theoretically maximium applied?
        private const float MaxLength = 20;

        public override string Name => "HitscanWeapon";
        public int Damage => _damage;
        public int BaseFireCost => _baseFireCost;
        public HitscanWeaponCapacitorComponent CapacitorComponent => _capacitorComponent;

        /// <inheritdoc/>
        public bool InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!eventArgs.Using.TryGetComponent(out PowerStorageComponent component)) return false;
            CapacitorComponent.FillFrom(component);
            return true;
        }

        /// <inheritdoc/>
        protected virtual void Fire(IEntity user, GridCoordinates clickLocation)
        {
            if (!CanFire()) return;

            var userPosition = user.Transform.WorldPosition; //Remember world positions are ephemeral and can only be used instantaneously
            var angle = new Angle(clickLocation.Position - userPosition);

            var rayCastResults = FireHitScan(userPosition, angle, user);

            float beamStrength = CapacitorComponent.GetChargeFrom(_baseFireCost) / _baseFireCost;
            float distance = MaxLength;
            bool hitSomething = rayCastResults.Any();

            if (hitSomething)
            {
                RayCastResults result = rayCastResults.First(); //The first result is guaranteed to be the closest one
                OnHit(result, beamStrength, user);
                distance = result.Distance;
            }

            CreateEnergyBeam(user, distance, angle, beamStrength);
            PlayFireSound();
        }

        protected virtual List<RayCastResults> FireHitScan(Vector2 userPosition, Angle angle, IEntity user)
        {
            var ray = new CollisionRay(userPosition, angle.ToVec(), (int) (CollisionGroup.Opaque));
            return IoCManager.Resolve<IPhysicsManager>().IntersectRay(user.Transform.MapID, ray, MaxLength, user, returnOnFirstHit: false).ToList();
        }

        /// <summary>
        /// when weapon is fired this function happens what happens if something is hit.
        /// </summary>
        /// <param name="ray"> Assumes atleast one entity hit by raycast.</param>
        /// <param name="damageModifier"></param>
        /// <param name="user">entity which fired the weapon</param>
        protected virtual void OnHit(RayCastResults ray, float damageModifier, IEntity? user = null)
        {
            if (ray.HitEntity.TryGetComponent(out DamageableComponent damage))
            {
                damage.TakeDamage(DamageType.Heat, (int)Math.Round(_damage * damageModifier, MidpointRounding.AwayFromZero), Owner, user);
                //I used Math.Round over Convert.toInt32, as toInt32 always rounds to
                //even numbers if halfway between two numbers, rather than rounding to nearest
            }
        }

        /// <summary>
        /// Creates a Laser Particle
        /// </summary>
        /// <param name="user">entity where the laser spawns from</param>
        /// <param name="distance">distance the laser travels</param>
        /// <param name="angle">Roated from east facing</param>
        /// <param name="beamStrength">Opacity Modifier</param>
        protected virtual void CreateEnergyBeam(IEntity user, float distance, Angle angle, float beamStrength)
        {
            var time = IoCManager.Resolve<IGameTiming>().CurTime;
            var offset = angle.ToVec() * distance / 2;
            var message = new EffectSystemMessage
            {
                EffectSprite = _spritename,
                Born = time,
                DeathTime = time + TimeSpan.FromSeconds(1),
                Size = new Vector2(distance, 1f),
                Coordinates = user.Transform.GridPosition.Translated(offset),
                Rotation = (float) angle.Theta,
                ColorDelta = new Vector4(0, 0, 0, -1500f),
                Color = Vector4.Multiply(new Vector4(255, 255, 255, 750), beamStrength),
                Shaded = false
            };
            EntitySystem.Get<EffectSystem>().CreateParticle(message);
        }

        //Make this public?

        /// <summary>
        /// Plays the gunshot sound when called
        /// </summary>
        protected void PlayFireSound() => EntitySystem.Get<AudioSystem>().PlayFromEntity(_fireSound, Owner, AudioParams.Default.WithVolume(-5));

        //Make this public?

        /// <summary>
        /// Checks to see weapon can fire
        /// </summary>
        protected virtual bool CanFire()
        {
            if (CapacitorComponent.CanDeductCharge(_lowerChargeLimit)) return true;
            return false;
        }
    }
}
