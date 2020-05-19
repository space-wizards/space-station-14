using Content.Server.GameObjects.Components.Network;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using System.Collections.Generic;
using System.Linq;

namespace Content.Server.GameObjects.Components.NewPower
{
    [RegisterComponent]
    public class LVNodeComponent : NetworkNodeComponent
    {
        public override string Name => "LVNode";

        public override NetworkType NetworkType => NetworkType.LVPower;

        protected override IEnumerable<NetworkNodeComponent> GetReachableNodes()
        {
            return Owner.GetComponent<SnapGridComponent>()
                .GetCardinalNeighborCells()
                .SelectMany(sgc => sgc.GetLocal())
                .Select(entity => entity.TryGetComponent<LVNodeComponent>(out var node) ? node : null)
                .Where(node => node != null);
        }
    }

    [RegisterComponent]
    public class MVNodeComponent : NetworkNodeComponent
    {
        public override string Name => "MVNode";

        public override NetworkType NetworkType => NetworkType.MVPower;

        protected override IEnumerable<NetworkNodeComponent> GetReachableNodes()
        {
            return Owner.GetComponent<SnapGridComponent>()
                .GetCardinalNeighborCells()
                .SelectMany(sgc => sgc.GetLocal())
                .Select(entity => entity.TryGetComponent<MVNodeComponent>(out var node) ? node : null)
                .Where(node => node != null);
        }
    }

    [RegisterComponent]
    public class HVNodeComponent : NetworkNodeComponent
    {
        public override string Name => "HVNode";

        public override NetworkType NetworkType => NetworkType.HVPower;

        protected override IEnumerable<NetworkNodeComponent> GetReachableNodes()
        {
            return Owner.GetComponent<SnapGridComponent>()
                .GetCardinalNeighborCells()
                .SelectMany(sgc => sgc.GetLocal())
                .Select(entity => entity.TryGetComponent<HVNodeComponent>(out var node) ? node : null)
                .Where(node => node != null);
        }
    }
}
