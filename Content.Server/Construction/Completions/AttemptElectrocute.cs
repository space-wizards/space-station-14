using System.Threading.Tasks;
using Content.Server.Electrocution;
using Content.Shared.Construction;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Construction.Completions
{
    [DataDefinition]
    public class AttemptElectrocute : IGraphAction
    {
        public async Task PerformAction(IEntity entity, IEntity? user)
        {
            if (user == null)
                return;

            EntitySystem.Get<ElectrocutionSystem>().TryDoElectrifiedAct(entity.Uid, user.Uid);
        }
    }
}
