using Content.Shared.Construction;
using JetBrains.Annotations;

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

            var xformSystem = entityManager.System<SharedTransformSystem>();
            if (Value)
                xformSystem.AnchorEntity((uid, transform));
            else
                xformSystem.Unanchor(uid, transform);
        }
    }
}
