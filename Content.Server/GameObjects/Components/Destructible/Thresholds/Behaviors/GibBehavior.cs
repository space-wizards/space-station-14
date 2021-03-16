using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Body;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Destructible.Thresholds.Behaviors
{
    [UsedImplicitly]
    [DataDefinition]
    public class GibBehavior : IThresholdBehavior
    {
        [DataField("recursive")] private bool _recursive = true;

        public void Execute(IEntity owner, DestructibleSystem system)
        {
            if (owner.TryGetComponent(out IBody? body))
            {
                body.Gib(_recursive);
            }
        }
    }
}
