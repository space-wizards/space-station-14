using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using System;
using System.Linq;

namespace Content.Server.GameObjects.Components.NewPower.ApcNetComponents
{
    /// <summary>
    ///     
    /// </summary>
    public class RemotePowerReceiverComponent : Component
    {
        public override string Name => "RemotePowerReceiver";

        public Action<bool> OnPowerStateChange;

        public bool Powered { get => _powered; set => SetPowered(value); }
        private bool _powered;

        public int PowerTransferRange { get => _powerTransferRange; set => SetPowerTransferRange(value); }
        private int _powerTransferRange;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _powerTransferRange, "powerTransferRange", 3);
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

        private void SetPowered(bool newPowered)
        {
            _powered = newPowered;
            OnPowerStateChange?.Invoke(_powered);
        }

        private void SetPowerTransferRange(int newPowerTransferRange)
        {
            _powerTransferRange = newPowerTransferRange;
        }
    }
}
