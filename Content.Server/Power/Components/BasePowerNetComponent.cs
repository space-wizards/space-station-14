using Content.Server.Power.NodeGroups;

namespace Content.Server.Power.Components
{
    public interface IBasePowerNetComponent : IBaseNetConnectorComponent<IPowerNet>
    {

    }

    public abstract partial class BasePowerNetComponent : BaseNetConnectorComponent<IPowerNet>
    {
    }
}
