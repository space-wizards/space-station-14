#nullable enable
using Content.Server.NodeContainer.NodeGroups;

namespace Content.Server.Power.Components
{
    public abstract class BasePowerNetComponent : BaseNetConnectorComponent<IPowerNet>
    {
        protected override IPowerNet NullNet => PowerNetNodeGroup.NullNet;
    }
}
