#nullable enable
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;

namespace Content.Server.GameObjects.Components.Power.ApcNetComponents
{
    public abstract class BaseApcNetComponent : BaseNetConnectorComponent<IApcNet>
    {
        protected override IApcNet NullNet => ApcNetNodeGroup.NullNet;
    }
}
