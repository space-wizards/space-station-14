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
            var transform = entityManager.GetComponent<TransformComponent>(uid);
            var xformSystem = entityManager.System<SharedTransformSystem>();

            if (!transform.Anchored)
                xformSystem.SetCoordinates(uid, xformSystem.SnapToGrid(transform.Coordinates));

            if (SouthRotation)
            {
                transform.LocalRotation = Angle.Zero;
            }
        }
    }
}
