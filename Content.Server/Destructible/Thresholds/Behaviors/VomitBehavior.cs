using Content.Server.Medical;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Server.Destructible.Thresholds.Behaviors;

[DataDefinition]
public sealed partial class VomitBehavior : IThresholdBehavior
{
    public void Execute(EntityUid uid, DestructibleSystem system, EntityUid? cause = null)
    {
        system.EntityManager.System<VomitSystem>().Vomit(uid);
    }
}
