using Content.Shared.Destructible.Thresholds.Behaviors;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Server.Destructible.Thresholds.Behaviors;

/// <summary>
/// Causes the drink/food to open when the destruction threshold is reached.
/// If it is already open nothing happens.
/// </summary>
[DataDefinition]
public sealed partial class OpenBehavior : EntitySystem, IThresholdBehavior
{
    [Dependency] private readonly OpenableSystem _openable = default!;

    public void Execute(EntityUid uid, EntityUid? cause = null)
    {
        _openable.TryOpen(uid);
    }
}
