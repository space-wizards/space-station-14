using Content.Shared.Destructible.Thresholds.Behaviors;
using Content.Shared.Medical;

namespace Content.Server.Destructible.Thresholds.Behaviors;

[DataDefinition]
public sealed partial class VomitBehavior : EntitySystem, IThresholdBehavior
{
    [Dependency] private readonly VomitSystem _vomit = default!;

    public void Execute(EntityUid uid, EntityUid? cause = null)
    {
        _vomit.Vomit(uid);
    }
}
