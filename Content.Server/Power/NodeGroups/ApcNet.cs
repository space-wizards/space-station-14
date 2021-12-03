using System.Collections.Generic;
using System.Linq;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.Pow3r;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Content.Server.Power.NodeGroups
{
    public interface IApcNet : IBasePowerNet
    {
        void AddApc(ApcComponent apc);

        void RemoveApc(ApcComponent apc);

        void AddPowerProvider(ApcPowerProviderComponent provider);

        void RemovePowerProvider(ApcPowerProviderComponent provider);

        void QueueNetworkReconnect();

        GridId? GridId { get; }
    }

    [NodeGroup(NodeGroupID.Apc)]
    [UsedImplicitly]
    public class ApcNet : BaseNetConnectorNodeGroup<IApcNet>, IApcNet
    {
        private readonly PowerNetSystem _powerNetSystem = EntitySystem.Get<PowerNetSystem>();

        [ViewVariables] public readonly List<ApcComponent> Apcs = new();
        [ViewVariables] public readonly List<ApcPowerProviderComponent> Providers = new();
        [ViewVariables] public readonly List<PowerConsumerComponent> Consumers = new();

        //Debug property
        [ViewVariables] private int TotalReceivers => Providers.Sum(provider => provider.LinkedReceivers.Count);

        [ViewVariables]
        private IEnumerable<ApcPowerReceiverComponent> AllReceivers =>
            Providers.SelectMany(provider => provider.LinkedReceivers);

        GridId? IApcNet.GridId => GridId;

        [ViewVariables]
        public PowerState.Network NetworkNode { get; } = new();

        public override void Initialize(Node sourceNode)
        {
            base.Initialize(sourceNode);

            _powerNetSystem.InitApcNet(this);
        }

        public override void AfterRemake(IEnumerable<IGrouping<INodeGroup?, Node>> newGroups)
        {
            base.AfterRemake(newGroups);

            _powerNetSystem.DestroyApcNet(this);
        }

        public void AddApc(ApcComponent apc)
        {
            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(apc.Owner, out PowerNetworkBatteryComponent? netBattery))
                netBattery.NetworkBattery.LinkedNetworkDischarging = default;

            QueueNetworkReconnect();
            Apcs.Add(apc);
        }

        public void RemoveApc(ApcComponent apc)
        {
            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(apc.Owner, out PowerNetworkBatteryComponent? netBattery))
                netBattery.NetworkBattery.LinkedNetworkDischarging = default;

            QueueNetworkReconnect();
            Apcs.Remove(apc);
        }

        public void AddPowerProvider(ApcPowerProviderComponent provider)
        {
            Providers.Add(provider);

            QueueNetworkReconnect();
        }

        public void RemovePowerProvider(ApcPowerProviderComponent provider)
        {
            Providers.Remove(provider);

            QueueNetworkReconnect();
        }

        public void AddConsumer(PowerConsumerComponent consumer)
        {
            consumer.NetworkLoad.LinkedNetwork = default;
            Consumers.Add(consumer);
            QueueNetworkReconnect();
        }

        public void RemoveConsumer(PowerConsumerComponent consumer)
        {
            consumer.NetworkLoad.LinkedNetwork = default;
            Consumers.Remove(consumer);
            QueueNetworkReconnect();
        }

        public void QueueNetworkReconnect()
        {
            _powerNetSystem.QueueReconnectApcNet(this);
        }

        protected override void SetNetConnectorNet(IBaseNetConnectorComponent<IApcNet> netConnectorComponent)
        {
            netConnectorComponent.Net = this;
        }
    }
}
