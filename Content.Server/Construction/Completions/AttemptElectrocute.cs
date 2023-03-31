using Content.Server.Electrocution;
using Content.Shared.Construction;

namespace Content.Server.Construction.Completions
{
    [DataDefinition]
    public sealed class AttemptElectrocute : IGraphAction
    {
        public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
        {
            if (userUid == null)
                return;

            entityManager.EntitySysManager.GetEntitySystem<ElectrocutionSystem>().TryDoElectrifiedAct(uid, userUid.Value);
        }
    }
}
