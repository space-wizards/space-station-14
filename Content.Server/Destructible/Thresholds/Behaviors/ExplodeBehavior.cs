using Content.Server.Explosion.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Destructible.Thresholds.Behaviors;
using Content.Shared.Explosion.Components;
using JetBrains.Annotations;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    /// <summary>
    ///     This behavior will trigger entities with <see cref="ExplosiveComponent"/> to go boom.
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class ExplodeBehavior : IThresholdBehavior
    {
        public void Execute(EntityUid owner,
            IDependencyCollection collection,
            EntityManager entManager,
            EntityUid? cause = null)
        {
            entManager.System<ExplosionSystem>().TriggerExplosive(owner, user:cause);
        }
    }
}
