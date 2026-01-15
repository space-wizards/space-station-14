using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds.Behaviors;
using Content.Shared.Medical;

namespace Content.Server.Destructible.Thresholds.Behaviors;

[DataDefinition]
public sealed partial class VomitBehavior : IThresholdBehavior
{
    [Dependency] private readonly VomitSystem _vomit = default!;

    public void Execute(EntityUid uid, SharedDestructibleSystem system, EntityUid? cause = null)
    {
        _vomit.Vomit(uid);
    }
}
