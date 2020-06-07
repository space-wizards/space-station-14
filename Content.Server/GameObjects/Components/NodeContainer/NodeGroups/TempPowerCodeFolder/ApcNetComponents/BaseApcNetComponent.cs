using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;

namespace Content.Server.GameObjects.Components.NewPower.ApcNetComponents
{
    public abstract class BaseApcNetComponent : BaseNetConnectorComponent<IApcNet>
    {
        protected override IApcNet NullNet => ApcNetNodeGroup.NullNet;
    }
}
