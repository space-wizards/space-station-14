using Content.Server.Body.Components;
using JetBrains.Annotations;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class GibBehavior : IThresholdBehavior
    {
        [DataField("recursive")] private bool _recursive = true;

        public void Execute(EntityUid owner, DestructibleSystem system)
        {
            if (system.EntityManager.TryGetComponent(owner, out BodyComponent? body))
            {
                system.BodySystem.Gib(owner, _recursive, body);
            }
        }
    }
}
