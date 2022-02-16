using Content.Server.Throwing;
using Content.Shared.Acts;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Server.Explosion.Components
{
    [RegisterComponent]
    public sealed class ExplosionLaunchedComponent : Component, IExAct
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        void IExAct.OnExplosion(ExplosionEventArgs eventArgs)
        {
            if (_entMan.Deleted(Owner))
                return;

            var sourceLocation = eventArgs.Source;
            var targetLocation = _entMan.GetComponent<TransformComponent>(eventArgs.Target).Coordinates;

            if (sourceLocation.Equals(targetLocation)) return;

            var offset = (targetLocation.ToMapPos(_entMan) - sourceLocation.ToMapPos(_entMan));

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
