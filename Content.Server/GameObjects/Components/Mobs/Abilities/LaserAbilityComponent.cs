using System;
using System.Linq;
using Content.Server.GameObjects.Components.Sound;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Mobs.Actions;
using Content.Shared.Physics;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Players;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Mobs.Abilities
{
    [RegisterComponent]
    public class LaserAbilityComponent : SharedLaserAbilityComponent
    {
#pragma warning disable 649
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
        [Dependency] private readonly IEntityManager _entityManager;
        [Dependency] private readonly IGameTiming _gameTiming;
#pragma warning restore 649

        private const float MaxLength = 20;

        string _spritename;
        private int _damage;
        private int _baseFireCost;
        private float _lowerChargeLimit;
        private string _fireSound;
        private TimeSpan _cooldown;
        private TimeSpan _start;
        private TimeSpan _end;

        public override void Initialize()
        {
            base.Initialize();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _spritename, "fireSprite", "Objects/Projectiles/laser.png");
            serializer.DataField(ref _damage, "damage", 10);
            serializer.DataField(ref _baseFireCost, "baseFireCost", 300);
            serializer.DataField(ref _lowerChargeLimit, "lowerChargeLimit", 10);
            serializer.DataField(ref _fireSound, "fireSound", "/Audio/Guns/Gunshots/laser.ogg");
            serializer.DataField(ref _cooldown, "cooldown", TimeSpan.FromSeconds(1));
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);

            switch (message)
            {
                case FireLaserMessage msg:
                {
                    if (_gameTiming.CurTime < _end)
                    {
                        return;
                    }

                    _start = _gameTiming.CurTime;
                    _end = _start + _cooldown;

                    Fire(msg.Coordinates);
                    SendNetworkMessage(new FireLaserCooldownMessage(_start, _end));
                    break;
                }
            }
        }

        private void Fire(GridCoordinates clickLocation)
        {
            var user = Owner;
            var userPosition = user.Transform.WorldPosition; //Remember world positions are ephemeral and can only be used instantaneously
            var angle = new Angle(clickLocation.Position - userPosition);

            var ray = new CollisionRay(userPosition, angle.ToVec(), (int)(CollisionGroup.Opaque));
            var rayCastResults = IoCManager.Resolve<IPhysicsManager>().IntersectRay(user.Transform.MapID, ray, MaxLength, user, returnOnFirstHit: false).ToList();

            //The first result is guaranteed to be the closest one
            if (rayCastResults.Count >= 1)
            {
                Hit(rayCastResults[0], user);
                AfterEffects(user, rayCastResults[0].Distance, angle);
            }
            else
            {
                AfterEffects(user, MaxLength, angle);
            }
        }

        protected virtual void Hit(RayCastResults ray, IEntity user = null)
        {
            if (ray.HitEntity != null && ray.HitEntity.TryGetComponent(out DamageableComponent damage))
            {
                damage.TakeDamage(DamageType.Heat, _damage, Owner, user);
            }
        }

        protected virtual void AfterEffects(IEntity user, float distance, Angle angle)
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
                Color = new Vector4(255, 255, 255, 750),

                Shaded = false
            };
            EntitySystem.Get<EffectSystem>().CreateParticle(message);
            EntitySystem.Get<AudioSystem>().Play(_fireSound, Owner, AudioParams.Default.WithVolume(-5));
        }
    }
}
