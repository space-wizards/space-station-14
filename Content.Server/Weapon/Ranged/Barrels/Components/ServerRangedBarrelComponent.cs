using System;
using Content.Shared.Sound;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Weapon.Ranged.Barrels.Components
{
    /// <summary>
    /// All of the ranged weapon components inherit from this to share mechanics like shooting etc.
    /// Only difference between them is how they retrieve a projectile to shoot (battery, magazine, etc.)
    /// </summary>
    [Friend(typeof(GunSystem))]
    public abstract class ServerRangedBarrelComponent : SharedRangedBarrelComponent, ISerializationHooks
    {
        public override FireRateSelector FireRateSelector => _fireRateSelector;

        [DataField("currentSelector")]
        private FireRateSelector _fireRateSelector = FireRateSelector.Safety;

        public override FireRateSelector AllRateSelectors => _fireRateSelector;

        [DataField("fireRate")]
        public override float FireRate { get; } = 2f;

        // _lastFire is when we actually fired (so if we hold the button then recoil doesn't build up if we're not firing)
        public TimeSpan LastFire;

        // Recoil / spray control
        [DataField("minAngle")]
        private float _minAngleDegrees;

        public Angle MinAngle { get; private set; }

        [DataField("maxAngle")]
        private float _maxAngleDegrees = 45;

        public Angle MaxAngle { get; private set; }

        public Angle CurrentAngle = Angle.Zero;

        [DataField("angleDecay")]
        private float _angleDecayDegrees = 20;

        /// <summary>
        /// How slowly the angle's theta decays per second in radians
        /// </summary>
        public float AngleDecay { get; private set; }

        [DataField("angleIncrease")]
        private float? _angleIncreaseDegrees;

        /// <summary>
        /// How quickly the angle's theta builds for every shot fired in radians
        /// </summary>
        public float AngleIncrease { get; private set; }

        // Multiplies the ammo spread to get the final spread of each pellet
        [DataField("ammoSpreadRatio")]
        public float SpreadRatio { get; private set; }

        [DataField("canMuzzleFlash")]
        public bool CanMuzzleFlash { get; } = true;

        // Sounds
        [DataField("soundGunshot", required: true)]
        public SoundSpecifier SoundGunshot { get; set; } = default!;

        [DataField("soundEmpty")]
        public SoundSpecifier SoundEmpty { get; } = new SoundPathSpecifier("/Audio/Weapons/Guns/Empty/empty.ogg");

        void ISerializationHooks.BeforeSerialization()
        {
            _minAngleDegrees = (float) (MinAngle.Degrees * 2);
            _maxAngleDegrees = (float) (MaxAngle.Degrees * 2);
            _angleIncreaseDegrees = MathF.Round(AngleIncrease / ((float) Math.PI / 180f), 2);
            AngleDecay = MathF.Round(AngleDecay / ((float) Math.PI / 180f), 2);
        }

        void ISerializationHooks.AfterDeserialization()
        {
            // This hard-to-read area's dealing with recoil
            // Use degrees in yaml as it's easier to read compared to "0.0125f"
            MinAngle = Angle.FromDegrees(_minAngleDegrees / 2f);

            // Random doubles it as it's +/- so uhh we'll just half it here for readability
            MaxAngle = Angle.FromDegrees(_maxAngleDegrees / 2f);

            _angleIncreaseDegrees ??= 40 / FireRate;
            AngleIncrease = _angleIncreaseDegrees.Value * (float) Math.PI / 180f;

            AngleDecay = _angleDecayDegrees * (float) Math.PI / 180f;

            // For simplicity we'll enforce it this way; ammo determines max spread
            if (SpreadRatio > 1.0f)
            {
                Logger.Error("SpreadRatio must be <= 1.0f for guns");
                throw new InvalidOperationException();
            }
        }
    }

    /// <summary>
    /// Raised on a gun when it fires projectiles.
    /// </summary>
    public sealed class GunShotEvent : EntityEventArgs
    {
        /// <summary>
        /// Uid of the entity that shot.
        /// </summary>
        public EntityUid Uid;

        public readonly EntityUid[] FiredProjectiles;

        public GunShotEvent(EntityUid[] firedProjectiles)
        {
            FiredProjectiles = firedProjectiles;
        }
    }

    /// <summary>
    /// Raised on ammo when it is fired.
    /// </summary>
    public sealed class AmmoShotEvent : EntityEventArgs
    {
        /// <summary>
        /// Uid of the entity that shot.
        /// </summary>
        public EntityUid Uid;

        public readonly EntityUid[] FiredProjectiles;

        public AmmoShotEvent(EntityUid[] firedProjectiles)
        {
            FiredProjectiles = firedProjectiles;
        }
    }
}
