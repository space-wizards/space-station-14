using Content.Shared.Body.Components;
using Content.Shared.Database;
using JetBrains.Annotations;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class GibBehavior : IThresholdBehavior
    {
        public LogImpact Impact => LogImpact.Extreme;

        public void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null)
        {
            system.Gibbing.Gib(owner);
        }
    }
}
