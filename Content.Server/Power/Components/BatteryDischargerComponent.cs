using Content.Server.Power.NodeGroups;

namespace Content.Server.Power.Components
{
    [RegisterComponent]
    public sealed class BatteryDischargerComponent : BasePowerNetComponent
    {
        protected override void AddSelfToNet(IPowerNet net)
        {
            net.AddDischarger(this);
        }

        protected override void RemoveSelfFromNet(IPowerNet net)
        {
            net.RemoveDischarger(this);
        }
    }
}
