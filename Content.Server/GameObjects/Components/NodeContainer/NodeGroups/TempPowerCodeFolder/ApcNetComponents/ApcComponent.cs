using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;

namespace Content.Server.GameObjects.Components.NewPower.ApcNetComponents
{
    public class ApcComponent : BaseApcNetComponent
    {
        public override string Name => "NewApc";

        private BatteryComponent _battery;

        public override void Initialize()
        {
            base.Initialize();
            _battery = Owner.GetComponent<BatteryComponent>();
        }

        protected override void AddSelfToNet(IApcNet apcNet)
        {
            apcNet.AddApc(this);
        }

        protected override void RemoveSelfFromNet(IApcNet apcNet)
        {
            apcNet.RemoveApc(this);
        }
    }
}
