using Content.Server.GameObjects.Components.Power.PowerNetComponents;
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Power;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.ViewVariables;
using System;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Robust.Shared.IoC;
using Robust.Shared.Interfaces.Timing;

namespace Content.Server.GameObjects.Components.Power.ApcNetComponents
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class ApcComponent : BaseApcNetComponent, IActivate
    {
        public override string Name => "Apc";

        [ViewVariables]
        public BatteryComponent Battery { get; private set; }

        public bool MainBreakerEnabled { get; private set; } = true;

        private BoundUserInterface _userInterface;

        private AppearanceComponent _appearance;

        private ApcChargeState _lastChargeState;

        private TimeSpan _lastChargeStateChange;

        private ApcExternalPowerState _lastExternalPowerState;

        private TimeSpan _lastExternalPowerStateChange;

        private float _lastCharge = 0f;

        private TimeSpan _lastChargeChange;

        private bool _uiDirty = true;

        private const float HighPowerThreshold = 0.9f;

        private const int VisualsChangeDelay = 1;

#pragma warning disable 649
        [Dependency] private readonly IGameTiming _gameTiming;
#pragma warning restore 649

        public override void Initialize()
        {
            base.Initialize();
            Battery = Owner.GetComponent<BatteryComponent>();
            _appearance = Owner.GetComponent<AppearanceComponent>();
            _userInterface = Owner.GetComponent<ServerUserInterfaceComponent>().GetBoundUserInterface(ApcUiKey.Key);
            _userInterface.OnReceiveMessage += UserInterfaceOnReceiveMessage;
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
                EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Machines/machine_switch.ogg", Owner, AudioParams.Default.WithVolume(-2f));
            }
        }

        public void Update()
        {
            var newState = CalcChargeState();
            if (newState != _lastChargeState && _lastChargeStateChange + TimeSpan.FromSeconds(VisualsChangeDelay) < _gameTiming.CurTime)
            {
                _lastChargeState = newState;
                _lastChargeStateChange = _gameTiming.CurTime;
                _appearance.SetData(ApcVisuals.ChargeState, newState);
            }
            var newCharge = Battery.CurrentCharge;
            if (newCharge != _lastCharge && _lastChargeChange + TimeSpan.FromSeconds(VisualsChangeDelay) < _gameTiming.CurTime)
            {
                _lastCharge = newCharge;
                _lastChargeChange = _gameTiming.CurTime;
                _uiDirty = true;
            }
            var extPowerState = CalcExtPowerState();
            if (extPowerState != _lastExternalPowerState && _lastExternalPowerStateChange + TimeSpan.FromSeconds(VisualsChangeDelay) < _gameTiming.CurTime)
            {
                _lastExternalPowerState = extPowerState;
                _lastExternalPowerStateChange = _gameTiming.CurTime;
                _uiDirty = true;
            }
            if (_uiDirty)
            {
                _userInterface.SetState(new ApcBoundInterfaceState(MainBreakerEnabled, extPowerState, newCharge / Battery.MaxCharge));
                _uiDirty = false;
            }
        }

        private ApcChargeState CalcChargeState()
        {
            var chargeFraction = Battery.CurrentCharge / Battery.MaxCharge;
            if (chargeFraction > HighPowerThreshold)
            {
                return ApcChargeState.Full;
            }
            var consumer = Owner.GetComponent<PowerConsumerComponent>();
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
            if (!Owner.TryGetComponent(out BatteryStorageComponent batteryStorage))
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
            if (!eventArgs.User.TryGetComponent(out IActorComponent actor))
            {
                return;
            }
            _userInterface.Open(actor.playerSession);
        }
    }
}
