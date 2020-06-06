using Content.Server.GameObjects.Components.NewPower.ApcNetComponents;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.NodeContainer.NodeGroups
{
    public interface IApcNet
    {
        void AddApc(ApcComponent apc);

        void RemoveApc(ApcComponent apc);

        void AddRemotePowerProvider(RemotePowerProviderComponent provider);

        void RemoveRemotePowerProvider(RemotePowerProviderComponent provider);
    }

    [NodeGroup(NodeGroupID.Apc)]
    class ApcNetNodeGroup : BaseNodeGroup, IApcNet
    {
        [ViewVariables]
        private readonly List<ApcComponent> _apcs = new List<ApcComponent>();

        public static readonly IApcNet NullNet = new NullApcNet();

        public void AddApc(ApcComponent apc)
        {
            _apcs.Add(apc);
        }

        public void RemoveApc(ApcComponent apc)
        {
            _apcs.Remove(apc);
        }

        public void AddRemotePowerProvider(RemotePowerProviderComponent provider)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveRemotePowerProvider(RemotePowerProviderComponent provider)
        {
            throw new System.NotImplementedException();
        }

        private class NullApcNet : IApcNet
        {
            public void AddApc(ApcComponent apc) { }
            public void AddRemotePowerProvider(RemotePowerProviderComponent provider) { }
            public void RemoveApc(ApcComponent apc) { }
            public void RemoveRemotePowerProvider(RemotePowerProviderComponent provider) { }
        }
    }
}
