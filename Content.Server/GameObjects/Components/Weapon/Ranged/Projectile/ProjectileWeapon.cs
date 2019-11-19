using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Projectiles;
using Content.Server.GameObjects.Components.Sound;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Projectile
{
    public abstract class ProjectileWeaponComponent : Component
    {
        private float _spreadStdDev = 3;
        private bool _spread = true;
        private string _soundGunshot;

#pragma warning disable 649
        [Dependency] private IRobustRandom _spreadRandom;
#pragma warning restore 649

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Spread
        {
            get => _spread;
            set => _spread = value;
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public float SpreadStdDev
        {
            get => _spreadStdDev;
            set => _spreadStdDev = value;
        }

        public override void Initialize()
        {
            base.Initialize();

            var rangedWeapon = Owner.GetComponent<RangedWeaponComponent>();
            rangedWeapon.FireHandler = Fire;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _spread, "spread", true);
            serializer.DataField(ref _spreadStdDev, "spreadstddev", 3);
            serializer.DataField(ref _soundGunshot, "sound_gunshot", "/Audio/Guns/Gunshots/smg.ogg");
        }

        private void Fire(IEntity user, GridCoordinates clickLocation)
        {
            var projectile = GetFiredProjectile();
            if (projectile == null)
            {
                return;
            }

            var userPosition = user.Transform.GridPosition; //Remember world positions are ephemeral and can only be used instantaneously
            var angle = new Angle(clickLocation.Position - userPosition.Position);

            if (user.TryGetComponent(out CameraRecoilComponent recoil))
            {
                var recoilVec = angle.ToVec() * -0.15f;
                recoil.Kick(recoilVec);
            }

            if (Spread)
            {
                angle += Angle.FromDegrees(_spreadRandom.NextGaussian(0, SpreadStdDev));
            }

            projectile.Transform.GridPosition = userPosition;

            //Give it the velocity we fire from this weapon, and make sure it doesn't shoot our character
            projectile.GetComponent<ProjectileComponent>().IgnoreEntity(user);
            var velocity = projectile.GetComponent<ProjectileComponent>().Velocity;

            //Give it the velocity this weapon gives to things it fires from itself
            projectile.GetComponent<PhysicsComponent>().LinearVelocity = angle.ToVec() * velocity;

            //Rotate the bullets sprite to the correct direction, from north facing I guess
            projectile.Transform.LocalRotation = angle.Theta;

            // Sound!
            Owner.GetComponent<SoundComponent>().Play(_soundGunshot);
        }

        /// <summary>
        ///     Try to get a projectile for firing. If null, nothing will be fired.
        /// </summary>
        protected abstract IEntity GetFiredProjectile();
    }

    public enum BallisticCaliber
    {
        Unspecified = 0,
        // .32
        A32,
        // .357
        A357,
        // .44
        A44,
        // .45mm
        A45mm,
        // .50 cal
        A50,
        // 5.56mm
        A556mm,
        // 6.5mm
        A65mm,
        // 7.62mm
        A762mm,
        // 9mm
        A9mm,
        // 10mm
        A10mm,
        // 20mm
        A20mm,
        // 24mm
        A24mm,
    }
}
