using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;

namespace Content.Server.GameObjects.Components.NewPower
{
    public abstract class BasePowerComponent : BaseNetConnectorComponent<IPowerNet>
    {
        protected sealed override IPowerNet GetNullNet()
        {
            return PowerNetNodeGroup.NullNet;
        }
    }
}
