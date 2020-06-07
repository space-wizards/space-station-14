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
#pragma warning disable CS8618 // Non-nullable field is uninitialized. 
#pragma warning disable IDE1006 // Naming Styles
        protected HitscanWeaponCapacitorComponent _capacitorComponent;
#pragma warning restore IDE1006 // Naming Styles
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

        //Make these public?
        private void PlayFireSound() => EntitySystem.Get<AudioSystem>().Play(_fireSound, Owner, AudioParams.Default.WithVolume(-5));

        private bool CanFire()
        {
            if (_capacitorComponent.CanDeductCharge(_lowerChargeLimit)) return true;
            return false;
        }


        public bool InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!eventArgs.Using.TryGetComponent(out PowerStorageComponent component)) return false;
            _capacitorComponent.FillFrom(component);
            return true;
        }

        protected virtual void Fire(IEntity user, GridCoordinates clickLocation)
        {
            if (!CanFire()) return;

            var userPosition = user.Transform.WorldPosition; //Remember world positions are ephemeral and can only be used instantaneously
            var angle = new Angle(clickLocation.Position - userPosition);

            var rayCastResults = FireHitScan(userPosition, angle, user);

            float beemStrength = _capacitorComponent.GetChargeFrom(_baseFireCost) / _baseFireCost;
            float distance = MaxLength;
            bool hitSomething = rayCastResults.Any();

            if (hitSomething)
            {
                RayCastResults result = rayCastResults.First(); //The first result is guaranteed to be the closest one
                OnHit(result, beemStrength, user);
                distance = result.Distance;
            }

            CreateEnergyBeam(user, distance, angle, beemStrength);
            PlayFireSound();
        }

        protected virtual List<RayCastResults> FireHitScan(Vector2 userPosition, Angle angle, IEntity user)
        {
            var ray = new CollisionRay(userPosition, angle.ToVec(), (int) (CollisionGroup.Opaque));
            return IoCManager.Resolve<IPhysicsManager>().IntersectRay(user.Transform.MapID, ray, MaxLength, user, returnOnFirstHit: false).ToList();
        }


        protected void OnHit(RayCastResults ray, float damageModifier, IEntity? user = null)
        {
            if (ray.HitEntity.TryGetComponent(out DamageableComponent damage))
            {
                damage.TakeDamage(DamageType.Heat, (int)Math.Round(_damage * damageModifier, MidpointRounding.AwayFromZero), Owner, user);
                //I used Math.Round over Convert.toInt32, as toInt32 always rounds to
                //even numbers if halfway between two numbers, rather than rounding to nearest
            }
        }

        protected virtual void CreateEnergyBeam(IEntity user, float distance, Angle angle, float beemStrength)
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
                //Rotated from east facing
                Rotation = (float) angle.Theta,
                ColorDelta = new Vector4(0, 0, 0, -1500f),
                Color = Vector4.Multiply(new Vector4(255, 255, 255, 750), beemStrength),

                Shaded = false
            };
            EntitySystem.Get<EffectSystem>().CreateParticle(message);
        }
    }
}
