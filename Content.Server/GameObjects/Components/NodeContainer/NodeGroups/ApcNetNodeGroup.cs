using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Content.Server.GameObjects.Components.Power;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.NodeContainer.NodeGroups
{
    public interface IApcNet
    {
        bool Powered { get; }

        void AddApc(ApcComponent apc);

        void RemoveApc(ApcComponent apc);

        void AddPowerProvider(PowerProviderComponent provider);

        void RemovePowerProvider(PowerProviderComponent provider);

        void UpdatePowerProviderReceivers(PowerProviderComponent provider, int oldLoad, int newLoad);

        void Update(float frameTime);
    }

    [NodeGroup(NodeGroupID.Apc)]
    public class ApcNetNodeGroup : BaseNetConnectorNodeGroup<BaseApcNetComponent, IApcNet>, IApcNet
    {
        [ViewVariables]
        private readonly Dictionary<ApcComponent, BatteryComponent> _apcBatteries = new();

        [ViewVariables]
        private readonly List<PowerProviderComponent> _providers = new();

        [ViewVariables]
        public bool Powered { get => _powered; private set => SetPowered(value); }
        private bool _powered = false;

        //Debug property
        [ViewVariables]
        private int TotalReceivers => _providers.SelectMany(provider => provider.LinkedReceivers).Count();

        [ViewVariables]
        private int TotalPowerReceiverLoad { get => _totalPowerReceiverLoad; set => SetTotalPowerReceiverLoad(value); }
        private int _totalPowerReceiverLoad = 0;

        public static readonly IApcNet NullNet = new NullApcNet();

        #region IApcNet Methods

        protected override void SetNetConnectorNet(BaseApcNetComponent netConnectorComponent)
        {
            netConnectorComponent.Net = this;
        }

        public void AddApc(ApcComponent apc)
        {
            if (!apc.Owner.TryGetComponent(out BatteryComponent battery))
            {
                return;
            }

            _apcBatteries.Add(apc, battery);
        }

        public void RemoveApc(ApcComponent apc)
        {
            _apcBatteries.Remove(apc);
        }

        public void AddPowerProvider(PowerProviderComponent provider)
        {
            _providers.Add(provider);

            foreach (var receiver in provider.LinkedReceivers)
            {
                TotalPowerReceiverLoad += receiver.Load;
            }
        }

        public void RemovePowerProvider(PowerProviderComponent provider)
        {
            _providers.Remove(provider);

            foreach (var receiver in provider.LinkedReceivers)
            {
                TotalPowerReceiverLoad -= receiver.Load;
            }
        }

        public void UpdatePowerProviderReceivers(PowerProviderComponent provider, int oldLoad, int newLoad)
        {
            DebugTools.Assert(_providers.Contains(provider));
            TotalPowerReceiverLoad -= oldLoad;
            TotalPowerReceiverLoad += newLoad;
        }

        public void Update(float frameTime)
        {
            var remainingPowerNeeded = TotalPowerReceiverLoad * frameTime;

            foreach (var apcBatteryPair in _apcBatteries)
            {
                var apc = apcBatteryPair.Key;

                if (!apc.MainBreakerEnabled)
                    continue;

                var battery = apcBatteryPair.Value;

                if (battery.CurrentCharge < remainingPowerNeeded)
                {
                    remainingPowerNeeded -= battery.CurrentCharge;
                    battery.CurrentCharge = 0;
                }
                else
                {
                    battery.UseCharge(remainingPowerNeeded);
                    remainingPowerNeeded = 0;
                }

                if (remainingPowerNeeded == 0)
                    break;
            }

            Powered = remainingPowerNeeded == 0;
        }

        private void SetPowered(bool powered)
        {
            if (powered != Powered)
            {
                _powered = powered;
                PoweredChanged();
            }
        }

        private void PoweredChanged()
        {
            foreach (var provider in _providers)
            {
                foreach (var receiver in provider.LinkedReceivers)
                {
                    receiver.ApcPowerChanged();
                }
            }
        }

        private void SetTotalPowerReceiverLoad(int totalPowerReceiverLoad)
        {
            DebugTools.Assert(totalPowerReceiverLoad >= 0, $"Expected load equal to or greater than 0, was {totalPowerReceiverLoad}");
            _totalPowerReceiverLoad = totalPowerReceiverLoad;
        }

        #endregion

        private class NullApcNet : IApcNet
        {
            /// <summary>
            ///     It is important that this returns false, so <see cref="PowerProviderComponent"/>s with a <see cref="NullApcNet"/> have no power.
            /// </summary>
            public bool Powered => false;

            public void AddApc(ApcComponent apc) { }
            public void AddPowerProvider(PowerProviderComponent provider) { }
            public void RemoveApc(ApcComponent apc) { }
            public void RemovePowerProvider(PowerProviderComponent provider) { }
            public void UpdatePowerProviderReceivers(PowerProviderComponent provider, int oldLoad, int newLoad) { }
            public void Update(float frameTime) { }
        }
    }
}
