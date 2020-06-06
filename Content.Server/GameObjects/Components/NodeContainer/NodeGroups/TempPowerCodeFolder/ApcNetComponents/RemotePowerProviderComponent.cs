using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using System.Linq;

namespace Content.Server.GameObjects.Components.NewPower.ApcNetComponents
{
    public class RemotePowerProviderComponent : BaseApcNetComponent
    {
        public override string Name => "RemotePowerProvider";

        public int PowerTransferRange { get => _powerReceiverRange; set => SetPowerReceiverRange(value); }
        private int _powerReceiverRange;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _powerReceiverRange, "powerTransferRange", 3);
        }

        public override void Initialize()
        {
            base.Initialize();
            var serverEntityManager = IoCManager.Resolve<IServerEntityManager>();
            var mapManager = IoCManager.Resolve<IMapManager>();
            var entitiesAll = serverEntityManager.GetEntitiesInRange(Owner.Transform.GridPosition, PowerTransferRange);
            var entitiesInRange = entitiesAll.Where(entity => entity.Transform.GridPosition.Distance(mapManager, Owner.Transform.GridPosition) < PowerTransferRange);
            var providers = entitiesInRange.Select(entity => entity.TryGetComponent<RemotePowerProviderComponent>(out var provider) ? provider : null)
                .Where(provider => provider != null);
        }

        protected override void AddSelfToNet(IApcNet apcNet)
        {
            throw new System.NotImplementedException();
        }

        protected override void RemoveSelfFromNet(IApcNet apcNet)
        {
            throw new System.NotImplementedException();
        }

        private void SetPowerReceiverRange(int newPowerReceiverRange)
        {
            _powerReceiverRange = newPowerReceiverRange;
        }
    }
}
