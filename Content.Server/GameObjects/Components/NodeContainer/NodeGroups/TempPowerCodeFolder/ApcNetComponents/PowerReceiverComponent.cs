using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;

namespace Content.Server.GameObjects.Components.NewPower.ApcNetComponents
{
    /// <summary>
    ///     Attempts to link with a nearby <see cref="IPowerProvider"/> so that it can receive power from a <see cref="IApcNet"/>.
    /// </summary>
    [RegisterComponent]
    public class PowerReceiverComponent : Component
    {
        public override string Name => "PowerReceiver";

        public Action<bool> OnPowerStateChange;

        [ViewVariables]
        public bool Powered { get => _powered; set => SetPowered(value); }
        private bool _powered;

        [ViewVariables(VVAccess.ReadWrite)]
        public int PowerReceptionRange { get => _powerReceptionRange; set => SetPowerReceptionRange(value); }
        private int _powerReceptionRange;

        [ViewVariables]
        public IPowerProvider Provider { get => _provider; set => SetProvider(value); }
        private IPowerProvider _provider = PowerProviderComponent.NullProvider;

        [ViewVariables]
        public bool NeedsProvider { get; private set; } = true;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _powerReceptionRange, "powerReceptionRange", 3);
        }

        public override void Initialize()
        {
            base.Initialize();
            if (!NeedsProvider)
            {
                return;
            }
            TryFindAndSetProvider();
        }

        private void TryFindAndSetProvider()
        {
            if (TryFindAvailableProvider(out var provider))
            {
                Provider = provider;
            }
        }

        private bool TryFindAvailableProvider(out IPowerProvider foundProvider)
        {
            var nearbyEntities = IoCManager.Resolve<IServerEntityManager>()
                .GetEntitiesInRange(Owner.Transform.GridPosition, PowerReceptionRange);
            var mapManager = IoCManager.Resolve<IMapManager>();
            foreach (var entity in nearbyEntities)
            {
                if (entity.TryGetComponent<PowerProviderComponent>(out var provider))
                {
                    var distanceToProvider = provider.Owner.Transform.GridPosition.Distance(mapManager, Owner.Transform.GridPosition);
                    if (distanceToProvider < Math.Min(PowerReceptionRange, provider.PowerTransferRange))
                    {
                        foundProvider = provider;
                        return true;
                    }
                }
            }
            foundProvider = default;
            return false;
        }

        public void ClearProvider()
        {
            _provider.RemoveReceiver(this);
            _provider = PowerProviderComponent.NullProvider;
            NeedsProvider = true;
        }

        private void SetProvider(IPowerProvider newProvider)
        {
            _provider.RemoveReceiver(this);
            _provider = newProvider;
            newProvider.AddReceiver(this);
            NeedsProvider = false;
        }

        private void SetPowered(bool newPowered)
        {
            _powered = newPowered;
            OnPowerStateChange?.Invoke(_powered);
        }

        private void SetPowerReceptionRange(int newPowerReceptionRange)
        {
            ClearProvider();
            _powerReceptionRange = newPowerReceptionRange;
            TryFindAndSetProvider();
        }
    }
}
