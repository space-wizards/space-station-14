using Content.Shared.Coordinates.Helpers;
using Content.Shared.Construction;
using JetBrains.Annotations;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class SnapToGrid : IGraphAction
    {
        [DataField("southRotation")] public bool SouthRotation { get; private set; }

        public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
        {
            var xform = entityManager.GetComponent<TransformComponent>(uid);
            var transform = entityManager.System<SharedTransformSystem>();

            if (!xform.Anchored)
                transform.SetCoordinates(uid, xform, xform.Coordinates.SnapToGrid(entityManager));

            if (SouthRotation)
                transform.SetLocalRotation(xform, Angle.Zero);
        }
    }
}
