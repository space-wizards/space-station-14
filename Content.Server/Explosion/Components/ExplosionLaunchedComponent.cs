using Content.Server.Throwing;
using Content.Shared.Acts;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Server.Explosion.Components
{
    [RegisterComponent]
    public class ExplosionLaunchedComponent : Component, IExAct
    {
        public override string Name => "ExplosionLaunched";

        void IExAct.OnExplosion(ExplosionEventArgs eventArgs)
        {
            if ((!IoCManager.Resolve<IEntityManager>().EntityExists(Owner.Uid) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(Owner.Uid).EntityLifeStage) >= EntityLifeStage.Deleted)
                return;

            var sourceLocation = eventArgs.Source;
            var targetLocation = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(eventArgs.Target).Coordinates;

            if (sourceLocation.Equals(targetLocation)) return;

            var offset = (targetLocation.ToMapPos(IoCManager.Resolve<IEntityManager>()) - sourceLocation.ToMapPos(IoCManager.Resolve<IEntityManager>()));

            //Don't throw if the direction is center (0,0)
            if (offset == Vector2.Zero) return;

            var direction = offset.Normalized;

            var throwForce = eventArgs.Severity switch
            {
                ExplosionSeverity.Heavy => 30,
                ExplosionSeverity.Light => 20,
                _ => 0,
            };

            Owner.TryThrow(direction, throwForce);
        }
    }
}
