using Content.Server.Power.NodeGroups;

namespace Content.Server.Power.Components
{
    [RegisterComponent]
    [ComponentProtoName("PowerProvider")]
    public sealed partial class ApcPowerProviderComponent : BaseApcNetComponent
    {
        [ViewVariables] public List<EntityUid> LinkedReceivers { get; } = new();

        public void AddReceiver(Entity<ApcPowerReceiverComponent> receiver)
        {
            LinkedReceivers.Add(receiver);
            receiver.Comp.NetworkLoad.LinkedNetwork = default;

            Net?.QueueNetworkReconnect();
        }

        public void RemoveReceiver(Entity<ApcPowerReceiverComponent> receiver)
        {
            LinkedReceivers.Remove(receiver);
            receiver.Comp.NetworkLoad.LinkedNetwork = default;

            Net?.QueueNetworkReconnect();
        }

        protected override void AddSelfToNet(IApcNet apcNet)
        {
            apcNet.AddPowerProvider(this);
        }

        protected override void RemoveSelfFromNet(IApcNet apcNet)
        {
            apcNet.RemovePowerProvider(this);
        }
    }
}
