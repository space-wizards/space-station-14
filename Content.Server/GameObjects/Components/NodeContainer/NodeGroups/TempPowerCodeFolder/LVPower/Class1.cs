using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;

namespace Content.Server.GameObjects.Components.NewPower
{
    public abstract class BaseLVPowerComponent : BaseNetConnectorComponent<IApcNet>
    {
        protected sealed override IApcNet GetNullNet()
        {
            return ApcNetNodeGroup.NullNet;
        }
    }
}
