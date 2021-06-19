#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.Pow3r;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.ViewVariables;

namespace Content.Server.Power.NodeGroups
{
    public interface IApcNet
    {
        void AddApc(ApcComponent apc);

        void RemoveApc(ApcComponent apc);

        void AddPowerProvider(ApcPowerProviderComponent provider);

        void RemovePowerProvider(ApcPowerProviderComponent provider);

        void QueueNetworkReconnect();

        PowerState.Network NetworkNode { get; }

        GridId? GridId { get; }
    }

    [NodeGroup(NodeGroupID.Apc)]
    [UsedImplicitly]
    public class ApcNet : BaseNetConnectorNodeGroup<BaseApcNetComponent, IApcNet>, IApcNet
    {
        private readonly PowerNetSystem _powerNetSystem = EntitySystem.Get<PowerNetSystem>();

        [ViewVariables] public readonly List<ApcComponent> Apcs = new();

        [ViewVariables] public readonly List<ApcPowerProviderComponent> Providers = new();

        //Debug property
        [ViewVariables] private int TotalReceivers => Providers.Sum(provider => provider.LinkedReceivers.Count);

        [ViewVariables]
        private IEnumerable<ApcPowerReceiverComponent> AllReceivers =>
            Providers.SelectMany(provider => provider.LinkedReceivers);

        GridId? IApcNet.GridId => GridId;

        [ViewVariables]
        public PowerState.Network NetworkNode { get; } = new();

        public static readonly IApcNet NullNet = new NullApcNet();

        public override void Initialize(Node sourceNode)
        {
            base.Initialize(sourceNode);

            _powerNetSystem.InitApcNet(this);
        }

        protected override void AfterRemake(IEnumerable<INodeGroup> newGroups)
        {
            base.AfterRemake(newGroups);

            StopUpdates();
        }

        protected override void OnGivingNodesForCombine(INodeGroup newGroup)
        {
            base.OnGivingNodesForCombine(newGroup);

            StopUpdates();
        }

        private void StopUpdates()
        {
            _powerNetSystem.DestroyApcNet(this);
        }

        protected override void SetNetConnectorNet(BaseApcNetComponent netConnectorComponent)
        {
            netConnectorComponent.Net = this;
        }

        public void AddApc(ApcComponent apc)
        {
            var netBattery = apc.Owner.GetComponent<PowerNetworkBatteryComponent>();
            netBattery.NetworkBattery.LinkedNetworkSupplying = default;
            _powerNetSystem.QueueReconnectApcNet(this);
            Apcs.Add(apc);
        }

        public void RemoveApc(ApcComponent apc)
        {
            var netBattery = apc.Owner.GetComponent<PowerNetworkBatteryComponent>();
            netBattery.NetworkBattery.LinkedNetworkSupplying = default;
            _powerNetSystem.QueueReconnectApcNet(this);
            Apcs.Remove(apc);
        }

        public void AddPowerProvider(ApcPowerProviderComponent provider)
        {
            Providers.Add(provider);

            _powerNetSystem.QueueReconnectApcNet(this);
        }

        public void RemovePowerProvider(ApcPowerProviderComponent provider)
        {
            Providers.Remove(provider);

            _powerNetSystem.QueueReconnectApcNet(this);
        }

        public void QueueNetworkReconnect()
        {
            _powerNetSystem.QueueReconnectApcNet(this);
        }

        private class NullApcNet : IApcNet
        {
            public GridId? GridId => default;
            public void AddApc(ApcComponent apc) { }
            public void AddPowerProvider(ApcPowerProviderComponent provider) { }
            public void RemoveApc(ApcComponent apc) { }
            public void RemovePowerProvider(ApcPowerProviderComponent provider) { }
            public void QueueNetworkReconnect() { }

            public PowerState.Network NetworkNode { get; } = new();
        }
    }
}
