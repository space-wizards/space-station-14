using System.Threading.Tasks;
using Content.Server.Coordinates.Helpers;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public class SnapToGrid : IGraphAction
    {
        [DataField("southRotation")] public bool SouthRotation { get; private set; } = false;

        public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
        {
            var transform = entityManager.GetComponent<TransformComponent>(uid);
            transform.Coordinates = transform.Coordinates.SnapToGrid(entityManager);

            if (SouthRotation)
            {
                transform.LocalRotation = Angle.Zero;
            }
        }
    }
}
