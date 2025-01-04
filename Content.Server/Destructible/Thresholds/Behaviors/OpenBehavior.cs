using Content.Shared.Destructible.Thresholds.Behaviors;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Server.Destructible.Thresholds.Behaviors;

/// <summary>
/// Causes the drink/food to open when the destruction threshold is reached.
/// If it is already open nothing happens.
/// </summary>
[DataDefinition]
public sealed partial class OpenBehavior : IThresholdBehavior
{
    public void Execute(EntityUid uid,
        IDependencyCollection collection,
        EntityManager entManager,
        EntityUid? cause = null)
    {
        var openable = entManager.System<OpenableSystem>();
        openable.TryOpen(uid);
    }
}
