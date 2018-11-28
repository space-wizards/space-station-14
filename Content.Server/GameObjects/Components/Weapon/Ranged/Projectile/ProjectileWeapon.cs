using Content.Server.GameObjects.Components.Projectiles;
using SS14.Server.GameObjects;
using SS14.Server.GameObjects.EntitySystems;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Map;
using SS14.Shared.Maths;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Projectile
{
    public class ProjectileWeaponComponent : RangedWeaponComponent
    {
        public override string Name => "ProjectileWeapon";

        private string _ProjectilePrototype = "ProjectileBullet";

        private float _velocity = 20f;

        protected override void Fire(IEntity user, GridLocalCoordinates clicklocation)
        {
            var userposition = user.GetComponent<ITransformComponent>().LocalPosition; //Remember world positions are ephemeral and can only be used instantaneously
            var angle = new Angle(clicklocation.Position - userposition.Position);

            var theta = angle.Theta;

            //Spawn the projectileprototype
            IEntity projectile = IoCManager.Resolve<IServerEntityManager>().ForceSpawnEntityAt(_ProjectilePrototype, userposition);

            //Give it the velocity we fire from this weapon, and make sure it doesn't shoot our character
            projectile.GetComponent<ProjectileComponent>().IgnoreEntity(user);

            //Give it the velocity this weapon gives to things it fires from itself
            projectile.GetComponent<PhysicsComponent>().LinearVelocity = angle.ToVec() * _velocity;

            //Rotate the bullets sprite to the correct direction, from north facing I guess
            projectile.GetComponent<ITransformComponent>().LocalRotation = angle.Theta;

            // Sound!
            IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<AudioSystem>().Play("/Audio/gunshot_c20.ogg");
        }
    }
}
