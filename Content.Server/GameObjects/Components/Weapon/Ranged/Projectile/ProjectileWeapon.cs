using System;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Projectiles;
using Content.Server.GameObjects.Components.Sound;
using SS14.Server.GameObjects;
using SS14.Server.GameObjects.EntitySystems;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Shared.Serialization;
using SS14.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Projectile
{
    public abstract class ProjectileWeaponComponent : Component
    {
        private float _velocity = 20f;
        private float _spreadStdDev = 3;
        private bool _spread = true;

        private Random _spreadRandom;

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

            _spreadRandom = new Random(Owner.Uid.GetHashCode() ^ DateTime.Now.GetHashCode());
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _spread, "spread", true);
            serializer.DataField(ref _spreadStdDev, "spreadstddev", 3);
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

            //Give it the velocity this weapon gives to things it fires from itself
            projectile.GetComponent<PhysicsComponent>().LinearVelocity = angle.ToVec() * _velocity;

            //Rotate the bullets sprite to the correct direction, from north facing I guess
            projectile.Transform.LocalRotation = angle.Theta;

            // Sound!
            Owner.GetComponent<SoundComponent>().Play("/Audio/gunshot_c20.ogg");
        }

        /// <summary>
        ///     Try to get a projectile for firing. If null, nothing will be fired.
        /// </summary>
        protected abstract IEntity GetFiredProjectile();
    }

    public enum BallisticCaliber
    {
        Unspecified = 0,
        A12mm,
    }
}
