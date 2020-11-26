using Content.Server.GameObjects.Components.Power;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;
using System;
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

        void Update(float frameTime);

        bool DisruptPower(TimeSpan disruptionLength, TimeSpan disruptionCooldown);
    }

    [NodeGroup(NodeGroupID.Apc)]
    public class ApcNetNodeGroup : BaseNetConnectorNodeGroup<BaseApcNetComponent, IApcNet>, IApcNet
    {
        [ViewVariables]
        private readonly Dictionary<ApcComponent, BatteryComponent> _apcBatteries = new Dictionary<ApcComponent, BatteryComponent>();

        [ViewVariables]
        private readonly Dictionary<PowerProviderComponent, List<PowerReceiverComponent>> _providerReceivers = new Dictionary<PowerProviderComponent, List<PowerReceiverComponent>>();

        [ViewVariables]
        public TimeSpan DisruptionEnd { get; private set; } = new();

        [ViewVariables]
        public TimeSpan DisruptionCooldownEnd { get; private set; } = new();

        [ViewVariables]
        private bool Disrupted => _gameTiming.CurTime <= DisruptionEnd;

        [ViewVariables]
        private bool DisruptionOnCooldown => _gameTiming.CurTime <= DisruptionCooldownEnd;

        [Dependency] private readonly IGameTiming _gameTiming = default!;

        //Debug property
        [ViewVariables]
        private int TotalReceivers => _providerReceivers.SelectMany(kvp => kvp.Value).Count();

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
            if (!_apcBatteries.Any())
            {
                foreach (var receiver in _providerReceivers.SelectMany(kvp => kvp.Value))
                {
                    receiver.HasApcPower = false;
                }
            }
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

        public void Update(float frameTime)
        {
            if (Disrupted)
                return;

            var totalCharge = 0.0;
            var totalMaxCharge = 0;
            foreach (var (apc, battery) in _apcBatteries)
            {
                if (!apc.MainBreakerEnabled)
                    continue;

                totalCharge += battery.CurrentCharge;
                totalMaxCharge += battery.MaxCharge;
            }

            foreach (var (_, receivers) in _providerReceivers)
            {
                foreach (var receiver in receivers)
                {
                    if (!receiver.NeedsPower || receiver.PowerDisabled)
                        continue;

                    receiver.HasApcPower = TryUsePower(receiver.Load * frameTime);
                }
            }
        }

        private bool TryUsePower(float neededCharge)
        {
            foreach (var (apc, battery) in _apcBatteries)
            {
                if (!apc.MainBreakerEnabled)
                    continue;

                if (battery.TryUseCharge(neededCharge)) //simplification - all power needed must come from one battery
                {
                    return true;
                }
            }
            return false;
        }

        public bool DisruptPower(TimeSpan disruptionLength, TimeSpan disruptionCooldown)
        {
            if (!DisruptionOnCooldown & !Disrupted)
            {
                DisruptPower();
                var curTime = _gameTiming.CurTime;
                DisruptionEnd = curTime + disruptionLength;
                DisruptionCooldownEnd = DisruptionEnd + disruptionCooldown;
                return true;
            }
            else
                return false;
        }

        private void DisruptPower()
        {
            foreach (var providerPair in _providerReceivers)
            {
                foreach (var receiver in providerPair.Value)
                {
                    receiver.HasApcPower = false;
                }
            }
        }

        #endregion

        private class NullApcNet : IApcNet
        {
            public void AddApc(ApcComponent apc) { }
            public void AddPowerProvider(PowerProviderComponent provider) { }
            public void RemoveApc(ApcComponent apc) { }
            public void RemovePowerProvider(PowerProviderComponent provider) { }
            public void UpdatePowerProviderReceivers(PowerProviderComponent provider) { }
            public void Update(float frameTime) { }
            public bool DisruptPower(TimeSpan disruptionLength, TimeSpan disruptionCooldown) { return false; }
        }
    }
}
