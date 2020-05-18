using Content.Server.GameObjects.Components.Network;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.NewPower
{
    public class PowerNetwork : BaseNetwork
    {
        public override NetworkType NetworkType { get; }

        [ViewVariables]
        public EntityUid DebugID { get; }

        public PowerNetwork(NetworkNodeComponent sourceNode)
        {
            NetworkType = sourceNode.NetworkType;
            DebugID = sourceNode.Owner.Uid;
        }
    }
}
