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

        public void Execute(IEntity owner, DestructibleSystem system)
        {
            if (owner.TryGetComponent(out SharedBodyComponent? body))
            {
                body.Gib(_recursive);
            }
        }
    }
}
