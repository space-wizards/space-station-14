using Content.Shared.Coordinates.Helpers;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class SnapToGrid : IGraphAction
    {
        [DataField("southRotation")] public bool SouthRotation { get; private set; }

        public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
        {
            var transform = entityManager.GetComponent<TransformComponent>(uid);
            var transformSystem = entityManager.System<SharedTransformSystem>();

            if (!transform.Anchored)
            {
                var mapSystem = entityManager.System<SharedMapSystem>();
                transformSystem.SetCoordinates(uid, transform, mapSystem.AlignToGrid(transform.Coordinates));
            }

            if (SouthRotation)
            {
                transform.LocalRotation = Angle.Zero;
            }
        }
    }
}
