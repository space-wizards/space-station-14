using JetBrains.Annotations;
using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Shared.Construction;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class DumpCanister : IGraphAction
    {
        public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
        {
            entityManager.EntitySysManager.GetEntitySystem<GasCanisterSystem>().PurgeContents(uid);
        }
    }
}
