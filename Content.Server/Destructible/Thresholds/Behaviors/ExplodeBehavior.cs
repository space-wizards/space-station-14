using Content.Server.Explosion.Components;
using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds.Behaviors;
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
        public void Execute(EntityUid owner, DestructibleBehaviorSystem system, EntityUid? cause = null)
        {
            system.ExplosionSystem.TriggerExplosive(owner, user:cause);
        }
    }
}
