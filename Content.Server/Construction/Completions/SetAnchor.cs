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

            if (transform.Anchored == Value)
                return;

            var sys = entityManager.System<SharedTransformSystem>();

            if (Value)
                sys.AnchorEntity(uid, transform);
            else
                sys.Unanchor(uid, transform);

        }
    }
}
