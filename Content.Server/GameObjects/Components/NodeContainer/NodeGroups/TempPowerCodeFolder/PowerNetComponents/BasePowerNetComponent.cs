using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;

namespace Content.Server.GameObjects.Components.NewPower.PowerNetComponents
{
    public abstract class BasePowerNetComponent : BaseNetConnectorComponent<IPowerNet>
    {
        protected sealed override IPowerNet GetNullNet()
        {
            return PowerNetNodeGroup.NullNet;
        }
    }
}
