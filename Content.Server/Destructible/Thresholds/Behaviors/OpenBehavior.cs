using Content.Shared.Nutrition.EntitySystems;

namespace Content.Server.Destructible.Thresholds.Behaviors;

/// <summary>
/// Causes the drink/food to open when the destruction threshold is reached.
/// If it is already open nothing happens.
/// </summary>
[DataDefinition]
public sealed partial class OpenBehavior : IThresholdBehavior
{
    public void Execute(EntityUid uid, DestructibleSystem system, EntityUid? cause = null)
    {
        var openable = EntitySystem.Get<OpenableSystem>();
        openable.TryOpen(uid);
    }
}
