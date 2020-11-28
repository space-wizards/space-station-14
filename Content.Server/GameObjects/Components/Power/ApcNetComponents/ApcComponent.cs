#nullable enable
using System;
using System.Linq;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Content.Server.GameObjects.Components.Power.PowerNetComponents;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Power;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power.ApcNetComponents
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class ApcComponent : BaseApcNetComponent, IActivate
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override string Name => "Apc";

        public bool MainBreakerEnabled { get; private set; } = true;

        public TimeSpan DisruptionLength { get; private set; }

        public TimeSpan DisruptionCooldown { get; private set; }

        private TimeSpan _lastUiStateUpdate = new();

        private ApcChargeState _lastChargeState;

        private ApcExternalPowerState _lastExternalPowerState;

        private float _lastCharge;

        private bool _uiDirty = true;

        private const float HighPowerThreshold = 0.9f;

        private TimeSpan _uiStateUpdateCooldown = TimeSpan.FromSeconds(1);

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(ApcUiKey.Key);

        public BatteryComponent? Battery => Owner.TryGetComponent(out BatteryComponent? batteryComponent) ? batteryComponent : null;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, x => DisruptionLength, "disruptionLength", TimeSpan.FromSeconds(5));
            serializer.DataField(this, x => DisruptionCooldown, "disruptionCooldown", TimeSpan.FromSeconds(5));
        }

        public override void Initialize()
        {
            base.Initialize();

            Owner.EnsureComponent<BatteryComponent>();
            Owner.EnsureComponent<PowerConsumerComponent>();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnReceiveMessage;
            }

            Update();
        }

        protected override void AddSelfToNet(IApcNet apcNet)
        {
            apcNet.AddApc(this);
        }

        protected override void RemoveSelfFromNet(IApcNet apcNet)
        {
            apcNet.RemoveApc(this);
        }

        private void UserInterfaceOnReceiveMessage(ServerBoundUserInterfaceMessage serverMsg)
        {
            if (serverMsg.Message is ApcToggleMainBreakerMessage)
            {
                MainBreakerEnabled = !MainBreakerEnabled;
                _uiDirty = true;
                SwitchSound();
            }
            else if (serverMsg.Message is ApcCyclePowerMessage)
            {
                Net.DisruptPower(DisruptionLength, DisruptionCooldown);
                _uiDirty = true;
                UpdateBoundUiState();
                SwitchSound();
            }
        }

        private void SwitchSound()
        {
            EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Machines/machine_switch.ogg", Owner, AudioParams.Default.WithVolume(-2f));
        }

        public void Update()
        {
            var curTime = _gameTiming.CurTime;
            if (_lastUiStateUpdate + _uiStateUpdateCooldown >= curTime)
                return;

            var newState = CalcChargeState();
            if (newState != _lastChargeState)
            {
                _lastChargeState = newState;

                if (Owner.TryGetComponent(out AppearanceComponent? appearance))
                {
                    appearance.SetData(ApcVisuals.ChargeState, newState);
                }
            }

            Owner.TryGetComponent(out BatteryComponent? battery);

            var newCharge = battery?.CurrentCharge;
            if (newCharge != null && newCharge != _lastCharge )
            {
                _lastCharge = newCharge.Value;
                _uiDirty = true;
            }

            var extPowerState = CalcExtPowerState();
            if (extPowerState != _lastExternalPowerState)
            {
                _lastExternalPowerState = extPowerState;
                _uiDirty = true;
            }

            if (Net.Disrupted || Net.DisruptionOnCooldown)
            {
                _uiDirty = true;
            }

            if (_uiDirty)
            {
                UpdateBoundUiState();
                _uiDirty = false;
                _lastUiStateUpdate = curTime;
            }
        }

        private void UpdateBoundUiState()
        {
            if (!Owner.TryGetComponent<BatteryComponent>(out var battery))
                return;

            UserInterface?.SetState(new ApcBoundInterfaceState(
                MainBreakerEnabled,
                CalcExtPowerState(),
                battery.CurrentCharge / battery.MaxCharge,
                Net.Disrupted, Net.RemainingDisruption,
                Net.DisruptionOnCooldown,
                Net.RemainingDisruptionCooldown));
        }

        private ApcChargeState CalcChargeState()
        {
            if (!Owner.TryGetComponent(out BatteryComponent? battery))
            {
                return ApcChargeState.Lack;
            }

            var chargeFraction = battery.CurrentCharge / battery.MaxCharge;

            if (chargeFraction > HighPowerThreshold)
            {
                return ApcChargeState.Full;
            }

            if (!Owner.TryGetComponent(out PowerConsumerComponent? consumer))
            {
                return ApcChargeState.Full;
            }

            if (consumer.DrawRate == consumer.ReceivedPower)
            {
                return ApcChargeState.Charging;
            }
            else
            {
                return ApcChargeState.Lack;
            }
        }

        private ApcExternalPowerState CalcExtPowerState()
        {
            if (!Owner.TryGetComponent(out BatteryStorageComponent? batteryStorage))
            {
                return ApcExternalPowerState.None;
            }
            var consumer = batteryStorage.Consumer;
            if (consumer.ReceivedPower == 0 && consumer.DrawRate != 0)
            {
                return ApcExternalPowerState.None;
            }
            else if (consumer.ReceivedPower < consumer.DrawRate)
            {
                return ApcExternalPowerState.Low;
            }
            else
            {
                return ApcExternalPowerState.Good;
            }
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent? actor))
            {
                return;
            }

            UserInterface?.Open(actor.playerSession);
        }
    }
}
