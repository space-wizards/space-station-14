using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds.Behaviors;
using Content.Shared.Medical;

namespace Content.Server.Destructible.Thresholds.Behaviors;

[DataDefinition]
public sealed partial class VomitBehavior : IThresholdBehavior
{
    public void Execute(EntityUid uid, DestructibleBehaviorSystem system, EntityUid? cause = null)
    {
        system.EntityManager.System<VomitSystem>().Vomit(uid);
    }
}
