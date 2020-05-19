using Content.Server.GameObjects.Components.Network;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using System.Collections.Generic;
using System.Linq;

namespace Content.Server.GameObjects.Components.NewPower
{
    [RegisterComponent]
    public class MVWireComponent : BaseNodeComponent
    {
        public override string Name => "MVWire";

        public override NetworkType NetworkType => NetworkType.MVPower;

        protected override IEnumerable<BaseNodeComponent> GetReachableNodes()
        {
            return Owner.GetComponent<SnapGridComponent>()
                .GetCardinalNeighborCells()
                .SelectMany(sgc => sgc.GetLocal())
                .Select(entity => entity.TryGetComponent<MVWireComponent>(out var node) ? node : null)
                .Where(node => node != null);
        }
    }
}
