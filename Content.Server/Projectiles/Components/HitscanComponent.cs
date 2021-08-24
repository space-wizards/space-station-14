using System;
using Content.Shared.Damage;
using Content.Shared.Physics;
using Content.Shared.Sound;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Server.Projectiles.Components
{
    /// <summary>
    /// Lasers etc.
    /// </summary>
    [RegisterComponent]
    public class HitscanComponent : Component
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override string Name => "Hitscan";
        public CollisionGroup CollisionMask => (CollisionGroup) _collisionMask;

<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
        public override string Name => "Hitscan";
        public CollisionGroup CollisionMask => (CollisionGroup) _collisionMask;
=======
>>>>>>> Bring refactor-damageablecomponent branch up-to-date with master (#4510)
=======
>>>>>>> refactor-damageablecomponent

        [DataField("layers")] //todo  WithFormat.Flags<CollisionLayer>()
        private int _collisionMask = (int) CollisionGroup.Opaque;
        [DataField("damage")]
<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
        private float _damage = 10f;
        public DamageType DamageType => _damageType;
        [DataField("damageType")]
        private DamageType _damageType = DamageType.Heat;
        public float MaxLength => 20.0f;
=======
        public float Damage { get; set; } = 10f;
		public float MaxLength => 20.0f;
>>>>>>> refactor-damageablecomponent

<<<<<<< refs/remotes/origin/master
        private TimeSpan _startTime;
        private TimeSpan _deathTime;
=======
        [DataField("damageType", required: true)]
        private string _damageTypeID = default!;

        private DamageTypePrototype _damageType => _prototypeManager.Index<DamageTypePrototype>(_damageTypeID);
>>>>>>> update damagecomponent across shared and server
=======
        public float Damage { get; set; } = 10f;
<<<<<<< refs/remotes/origin/master
>>>>>>> Refactor damageablecomponent update (#4406)

        public float ColorModifier { get; set; } = 1.0f;
<<<<<<< HEAD
        [DataField("spriteName")]
=======
		public float MaxLength => 20.0f;

        private TimeSpan _startTime;
        private TimeSpan _deathTime;

        public float ColorModifier { get; set; } = 1.0f;
		[DataField("spriteName")]
>>>>>>> Bring refactor-damageablecomponent branch up-to-date with master (#4510)
=======
		[DataField("spriteName")]
>>>>>>> refactor-damageablecomponent
        private string _spriteName = "Objects/Weapons/Guns/Projectiles/laser.png";
        [DataField("muzzleFlash")]
        private string? _muzzleFlash;
        [DataField("impactFlash")]
        private string? _impactFlash;
        [DataField("soundHitWall")]
<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
<<<<<<< refs/remotes/origin/master
        private SoundSpecifier _soundHitWall = new SoundPathSpecifier("/Audio/Weapons/Guns/Hits/laser_sear_wall.ogg");
=======
        private string _soundHitWall = "/Audio/Weapons/Guns/Hits/laser_sear_wall.ogg";
        [DataField("spriteName")]
        private string _spriteName = "Objects/Weapons/Guns/Projectiles/laser.png";
=======
        private SoundSpecifier _soundHitWall = new SoundPathSpecifier("/Audio/Weapons/Guns/Hits/laser_sear_wall.ogg");
>>>>>>> Bring refactor-damageablecomponent branch up-to-date with master (#4510)
=======
        private SoundSpecifier _soundHitWall = new SoundPathSpecifier("/Audio/Weapons/Guns/Hits/laser_sear_wall.ogg");
>>>>>>> refactor-damageablecomponent


        // TODO PROTOTYPE Replace this datafield variable with prototype references, once they are supported.
        // Also remove Initialize override, if no longer needed.
        [DataField("damageType")]
        private readonly string _damageTypeID = "Piercing";
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageTypePrototype DamageType = default!;
        protected override void Initialize()
        {
            base.Initialize();
            DamageType = IoCManager.Resolve<IPrototypeManager>().Index<DamageTypePrototype>(_damageTypeID);
        }
<<<<<<< HEAD
>>>>>>> update damagecomponent across shared and server
=======
>>>>>>> refactor-damageablecomponent

        public void FireEffects(IEntity user, float distance, Angle angle, IEntity? hitEntity = null)
        {
            var effectSystem = EntitySystem.Get<EffectSystem>();
            _startTime = _gameTiming.CurTime;
            _deathTime = _startTime + TimeSpan.FromSeconds(1);

            var afterEffect = AfterEffects(user.Transform.Coordinates, angle, distance, 1.0f);
            if (afterEffect != null)
            {
                effectSystem.CreateParticle(afterEffect);
            }

            // if we're too close we'll stop the impact and muzzle / impact sprites from clipping
            if (distance > 1.0f)
            {
                var impactEffect = ImpactFlash(distance, angle);
                if (impactEffect != null)
                {
                    effectSystem.CreateParticle(impactEffect);
                }

                var muzzleEffect = MuzzleFlash(user.Transform.Coordinates, angle);
                if (muzzleEffect != null)
                {
                    effectSystem.CreateParticle(muzzleEffect);
                }
            }

            if (hitEntity != null && _soundHitWall != null)
            {
                // TODO: No wall component so ?
                var offset = angle.ToVec().Normalized / 2;
                var coordinates = user.Transform.Coordinates.Offset(offset);
                SoundSystem.Play(Filter.Pvs(coordinates), _soundHitWall.GetSound(), coordinates);
            }

            Owner.SpawnTimer((int) _deathTime.TotalMilliseconds, () =>
            {
                if (!Owner.Deleted)
                {
                    Owner.Delete();
                }
            });
        }

        private EffectSystemMessage? MuzzleFlash(EntityCoordinates grid, Angle angle)
        {
            if (_muzzleFlash == null)
            {
                return null;
            }

            var offset = angle.ToVec().Normalized / 2;

            var message = new EffectSystemMessage
            {
                EffectSprite = _muzzleFlash,
                Born = _startTime,
                DeathTime = _deathTime,
                Coordinates = grid.Offset(offset),
                //Rotated from east facing
                Rotation = (float) angle.Theta,
                Color = Vector4.Multiply(new Vector4(255, 255, 255, 750), ColorModifier),
                ColorDelta = new Vector4(0, 0, 0, -1500f),
                Shaded = false
            };

            return message;
        }

        private EffectSystemMessage AfterEffects(EntityCoordinates origin, Angle angle, float distance, float offset = 0.0f)
        {
            var midPointOffset = angle.ToVec() * distance / 2;
            var message = new EffectSystemMessage
            {
                EffectSprite = _spriteName,
                Born = _startTime,
                DeathTime = _deathTime,
                Size = new Vector2(distance - offset, 1f),
                Coordinates = origin.Offset(midPointOffset),
                //Rotated from east facing
                Rotation = (float) angle.Theta,
                Color = Vector4.Multiply(new Vector4(255, 255, 255, 750), ColorModifier),
                ColorDelta = new Vector4(0, 0, 0, -1500f),

                Shaded = false
            };

            return message;
        }

        private EffectSystemMessage? ImpactFlash(float distance, Angle angle)
        {
            if (_impactFlash == null)
            {
                return null;
            }

            var message = new EffectSystemMessage
            {
                EffectSprite = _impactFlash,
                Born = _startTime,
                DeathTime = _deathTime,
                Coordinates = Owner.Transform.Coordinates.Offset(angle.ToVec() * distance),
                //Rotated from east facing
                Rotation = (float) angle.FlipPositive(),
                Color = Vector4.Multiply(new Vector4(255, 255, 255, 750), ColorModifier),
                ColorDelta = new Vector4(0, 0, 0, -1500f),
                Shaded = false
            };

            return message;
        }
    }
}
