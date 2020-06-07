using Content.Server.GameObjects.Components.NewPower.ApcNetComponents;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;
using System.Linq;

namespace Content.Server.GameObjects.Components.NodeContainer.NodeGroups
{
    public interface IApcNet
    {
        void AddApc(ApcComponent apc);

        void RemoveApc(ApcComponent apc);

        void AddRemotePowerProvider(PowerProviderComponent provider);

        void RemoveRemotePowerProvider(PowerProviderComponent provider);
    }

    [NodeGroup(NodeGroupID.Apc)]
    class ApcNetNodeGroup : BaseNodeGroup, IApcNet
    {
        [ViewVariables]
        private readonly List<ApcComponent> _apcs = new List<ApcComponent>();

        [ViewVariables]
        private readonly Dictionary<PowerProviderComponent, List<PowerReceiverComponent>> _receiverByProvider = new Dictionary<PowerProviderComponent, List<PowerReceiverComponent>>();

        public static readonly IApcNet NullNet = new NullApcNet();

        public void AddApc(ApcComponent apc)
        {
            _apcs.Add(apc);
        }

        public void RemoveApc(ApcComponent apc)
        {
            _apcs.Remove(apc);
        }

        public void AddRemotePowerProvider(PowerProviderComponent provider)
        {
            _receiverByProvider.Add(provider, provider.LinkedReceivers.ToList());
        }

        public void RemoveRemotePowerProvider(PowerProviderComponent provider)
        {
            _receiverByProvider.Remove(provider);
        }

        public void Update(float frameTime)
        {

        }

        private class NullApcNet : IApcNet
        {
            public void AddApc(ApcComponent apc) { }
            public void AddRemotePowerProvider(PowerProviderComponent provider) { }
            public void RemoveApc(ApcComponent apc) { }
            public void RemoveRemotePowerProvider(PowerProviderComponent provider) { }
        }
    }
}
