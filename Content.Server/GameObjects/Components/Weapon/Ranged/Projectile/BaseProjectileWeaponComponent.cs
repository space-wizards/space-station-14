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
using System.Collections.Generic;
using Robust.Shared.Physics;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Projectile
{
    /// <summary>
    ///     Methods to shoot projectiles.
    /// </summary>
    public abstract class BaseProjectileWeaponComponent : Component
    {
        private string _soundGunshot;
        [ViewVariables(VVAccess.ReadWrite)]
        public string SoundGunshot
        { get => _soundGunshot; set => _soundGunshot = value; }

#pragma warning disable 649
        [Dependency] private IRobustRandom _spreadRandom;
#pragma warning restore 649

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _soundGunshot, "sound_gunshot", "/Audio/Guns/Gunshots/smg.ogg");
        }

        /// <summary>
        ///     Fires projectile from an entity at a coordinate.
        /// </summary>
        protected void FireAtCoord(IEntity source, GridCoordinates coord, string projectileType, double spreadStdDev, int projectilesFired = 1, double evenSpreadAngle = 0, float velocity = 0)
        {
            var angle = GetAngleFromClickLocation(source, coord);
            FireAtAngle(source, angle, projectileType, spreadStdDev, projectilesFired, evenSpreadAngle, velocity);
        }

        /// <summary>
        ///     Fires projectile in the direction of an angle.
        /// </summary>
        protected void FireAtAngle(IEntity source, Angle angle, string projectileType = null, double spreadStdDev = 0, int projectilesFired = 1, double evenSpreadAngle = 0, float velocity = 0)
        {
            List<Angle> sprayanglechange = null;
            if (evenSpreadAngle != 0 & projectilesFired > 1)
            {
                sprayanglechange = Linspace(-evenSpreadAngle/2, evenSpreadAngle/2, projectilesFired);
            }
            for (var i = 1; i <= projectilesFired; i++)
            {
                Angle finalangle = angle + Angle.FromDegrees(_spreadRandom.NextGaussian(0, spreadStdDev)) + (sprayanglechange != null ? sprayanglechange[i - 1] : 0);
                var projectile = Owner.EntityManager.SpawnEntity(projectileType, Owner.Transform.GridPosition);
                projectile.Transform.GridPosition = source.Transform.GridPosition; //move projectile to entity it is being fired from
                projectile.GetComponent<ProjectileComponent>().IgnoreEntity(source);//make sure it doesn't hit the source entity
                var finalvelocity = projectile.GetComponent<ProjectileComponent>().Velocity + velocity;//add velocity
                var physicsComponent = projectile.GetComponent<PhysicsComponent>();
                physicsComponent.Status = BodyStatus.InAir;
                physicsComponent.LinearVelocity = finalangle.ToVec() * finalvelocity;//Rotate the bullets sprite to the correct direction
                projectile.Transform.LocalRotation = finalangle.Theta;
            }
            PlayFireSound();
            if (source.TryGetComponent(out CameraRecoilComponent recoil))
            {
                var recoilVec = angle.ToVec() * -0.15f;
                recoil.Kick(recoilVec);
            }
        }

        private void PlayFireSound() => Owner.GetComponent<SoundComponent>().Play(_soundGunshot);

        /// <summary>
        ///     Gets the angle from an entity to a coordinate.
        /// </summary>
        protected Angle GetAngleFromClickLocation(IEntity source, GridCoordinates clickLocation) => new Angle(clickLocation.Position - source.Transform.GridPosition.Position);

        /// <summary>
        ///     Returns a list of numbers that form a set of equal intervals between the start and end value. Used to calculate shotgun spread angles.
        /// </summary>
        protected List<Angle> Linspace(double start, double end, int intervals)
        {
            var linspace = new List<Angle> { };
            for (var i = 0; i <= intervals - 1; i++)
            {
                linspace.Add(Angle.FromDegrees(start + (end - start) * i / (intervals - 1)));
            }
            return linspace;
        }
    }
}
