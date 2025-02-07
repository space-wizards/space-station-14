using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class SetAnchor : IGraphAction
    {
        [DataField("value")] public bool Value { get; private set; } = true;

        public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
        {
            var transform = entityManager.GetComponent<TransformComponent>(uid);

            if (Value == transform.Anchored)
                return;

            var mapManager = IoCManager.Resolve<IMapManager>();
            var transformSystem = entityManager.System<SharedTransformSystem>();

            if (Value && mapManager.TryFindGridAt(transformSystem.GetMapCoordinates(transform), out var gridUid, out var grid))
                transformSystem.AnchorEntity((uid, transform), (gridUid, grid));
            else
                transformSystem.Unanchor(uid, transform);
        }
    }
}
