using JetBrains.Annotations;
using Content.Shared.Database;
using Content.Shared.Destructible.Thresholds.Behaviors;
using Content.Shared.Gibbing;

namespace Content.Server.Destructible.Thresholds.Behaviors;

[UsedImplicitly]
[DataDefinition]
public sealed partial class GibBehavior : EntitySystem, IThresholdBehavior
{
    [Dependency] private readonly GibbingSystem _gibbing = default!;

    /// <summary>
    /// Whether to gib recursively.
    /// </summary>
    [DataField]
    public bool Recursive = true;

    public LogImpact Impact => LogImpact.Extreme;

    public void Execute(EntityUid owner, EntityUid? cause = null)
    {
        _gibbing.Gib(owner, Recursive);
    }
}

