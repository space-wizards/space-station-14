using Content.Shared.Body.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    [UsedImplicitly]
    [DataDefinition]
    public class GibBehavior : IThresholdBehavior
    {
        [DataField("recursive")] private bool _recursive = true;

        public void Execute(EntityUid owner, DestructibleSystem system)
        {
            if (system.EntityManager.TryGetComponent(owner, out SharedBodyComponent? body))
            {
                body.Gib(_recursive);
            }
        }
    }
}
