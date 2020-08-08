using Content.Server.Atmos;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.NodeContainer.NodeGroups
{
    public interface IPipeNet
    {
        public GasMixture ContainedGas { get; }
    }

    [NodeGroup(NodeGroupID.Pipe)]
    public class PipeNet : BaseNodeGroup, IPipeNet
    {
        [ViewVariables]
        public GasMixture ContainedGas { get; private set; } = new GasMixture();
    }
}
