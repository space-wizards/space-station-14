using Content.Shared.Construction;
using JetBrains.Annotations;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class DeleteEntity : IGraphAction
    {
        public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
        {
            entityManager.DeleteEntity(uid);
        }
    }
}
