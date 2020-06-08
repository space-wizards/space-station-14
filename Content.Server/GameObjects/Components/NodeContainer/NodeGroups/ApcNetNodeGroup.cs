using Content.Server.GameObjects.Components.NewPower;
using Content.Server.GameObjects.Components.NewPower.ApcNetComponents;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Content.Server.GameObjects.Components.NodeContainer.NodeGroups
{
    public interface IApcNet
    {
        void AddApc(ApcComponent apc);

        void RemoveApc(ApcComponent apc);

        void AddPowerProvider(PowerProviderComponent provider);

        void RemovePowerProvider(PowerProviderComponent provider);

        void UpdatePowerProviderReceivers(PowerProviderComponent provider);
    }

    [NodeGroup(NodeGroupID.Apc)]
    public class ApcNetNodeGroup : BaseNetConnectorNodeGroup<BaseApcNetComponent, IApcNet>, IApcNet
    {
        [ViewVariables]
        private readonly Dictionary<ApcComponent, BatteryComponent> _apcBatteries = new Dictionary<ApcComponent, BatteryComponent>();

        [ViewVariables]
        private readonly Dictionary<PowerProviderComponent, List<PowerReceiverComponent>> _providerReceivers = new Dictionary<PowerProviderComponent, List<PowerReceiverComponent>>();

        [ViewVariables]
        private int TotalReceivers => _providerReceivers.SelectMany(kvp => kvp.Value).Count();

        public static readonly IApcNet NullNet = new NullApcNet();

        protected override void SetNetConnectorNet(BaseApcNetComponent netConnectorComponent)
        {
            netConnectorComponent.Net = this;
        }

        #region IApcNet Methods

        public void AddApc(ApcComponent apc)
        {
            _apcBatteries.Add(apc, apc.Battery);
        }

        public void RemoveApc(ApcComponent apc)
        {
            _apcBatteries.Remove(apc);
        }

        public void AddPowerProvider(PowerProviderComponent provider)
        {
            _providerReceivers.Add(provider, provider.LinkedReceivers.ToList());
        }

        public void RemovePowerProvider(PowerProviderComponent provider)
        {
            _providerReceivers.Remove(provider);
        }

        public void UpdatePowerProviderReceivers(PowerProviderComponent provider)
        {
            Debug.Assert(_providerReceivers.ContainsKey(provider));
            _providerReceivers[provider] = provider.LinkedReceivers.ToList();
        }

        #endregion

        private class NullApcNet : IApcNet
        {
            public void AddApc(ApcComponent apc) { }
            public void AddPowerProvider(PowerProviderComponent provider) { }
            public void RemoveApc(ApcComponent apc) { }
            public void RemovePowerProvider(PowerProviderComponent provider) { }
            public void UpdatePowerProviderReceivers(PowerProviderComponent provider) { }
        }
    }
}
