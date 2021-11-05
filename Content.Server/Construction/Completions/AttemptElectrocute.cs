using Content.Server.Electrocution;
using Content.Shared.Construction;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Construction.Completions
{
    [DataDefinition]
    public class AttemptElectrocute : IGraphAction
    {
        public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
        {
            if (userUid == null)
                return;

            EntitySystem.Get<ElectrocutionSystem>().TryDoElectrifiedAct(uid, userUid.Value);
        }
    }
}
