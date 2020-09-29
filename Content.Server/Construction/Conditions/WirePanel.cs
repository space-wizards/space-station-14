using System.Threading.Tasks;
using Content.Server.GameObjects.Components;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    public class WirePanel : IEdgeCondition
    {
        public bool Open { get; private set; }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Open, "open", true);
        }

        public async Task<bool> Condition(IEntity entity)
        {
            if (!entity.TryGetComponent(out WiresComponent wires)) return false;

            return wires.IsPanelOpen == Open;
        }
    }
}
