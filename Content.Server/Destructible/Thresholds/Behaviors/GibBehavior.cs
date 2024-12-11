using Content.Server.Body.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Destructible.Thresholds.Behaviors;
using JetBrains.Annotations;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class GibBehavior : IThresholdBehavior
    {
        [DataField("recursive")] private bool _recursive = true;

        public void Execute(EntityUid owner,
            IDependencyCollection collection,
            EntityManager entManager,
            EntityUid? cause = null)
        {
            if (entManager.TryGetComponent(owner, out BodyComponent? body))
            {
                entManager.System<BodySystem>().GibBody(owner, _recursive, body);
            }
        }
    }
}
