using Content.Server.Explosion.Components;
using JetBrains.Annotations;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    /// <summary>
    ///     This behavior will trigger entities with <see cref="ExplosiveComponent"/> to go boom.
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public sealed class ExplodeBehavior : IThresholdBehavior
    {
        public void Execute(EntityUid owner, DestructibleSystem system)
        {
            system.ExplosionSystem.TriggerExplosive(owner);
        }
    }
}
