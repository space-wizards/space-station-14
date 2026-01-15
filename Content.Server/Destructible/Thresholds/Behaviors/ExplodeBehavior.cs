using JetBrains.Annotations;
using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds.Behaviors;
using Content.Shared.Explosion.Components;
using Content.Shared.Explosion.EntitySystems;

namespace Content.Server.Destructible.Thresholds.Behaviors;

/// <summary>
///     This behavior will trigger entities with <see cref="ExplosiveComponent"/> to go boom.
/// </summary>
[UsedImplicitly]
[DataDefinition]
public sealed partial class ExplodeBehavior : IThresholdBehavior
{
    [Dependency] private readonly SharedExplosionSystem _explosionSystem = default!;

    public void Execute(EntityUid owner, SharedDestructibleSystem system, EntityUid? cause = null)
    {
        _explosionSystem.TriggerExplosive(owner, user: cause);
    }
}
