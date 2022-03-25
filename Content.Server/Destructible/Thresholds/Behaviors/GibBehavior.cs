using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

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
                var bodySys = EntitySystem.Get<BodySystem>();
                bodySys.Gib(owner, _recursive, body);
            }
        }
    }
}
