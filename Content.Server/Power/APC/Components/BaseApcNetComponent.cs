#nullable enable
using Content.Server.Power.Components;

namespace Content.Server.APC.Components
{
    public abstract class BaseApcNetComponent : BaseNetConnectorComponent<IApcNet>
    {
        protected override IApcNet NullNet => ApcNetNodeGroup.NullNet;
    }
}
