using Content.Server.Power.NodeGroups;

namespace Content.Server.Power.Components
{
    public interface IBasePowerNetComponent : IBaseNetConnectorComponent<IPowerNet>
    {

    }

    public abstract class BasePowerNetComponent : BaseNetConnectorComponent<IPowerNet>
    {
    }
}
