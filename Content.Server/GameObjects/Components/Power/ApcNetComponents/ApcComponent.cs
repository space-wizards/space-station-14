using Content.Server.GameObjects.Components.NewPower.PowerNetComponents;
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

namespace Content.Server.GameObjects.Components.NewPower.ApcNetComponents
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class ApcComponent : BaseApcNetComponent, IActivate
    {
        public override string Name => "NewApc";

        [ViewVariables]
        public BatteryComponent Battery { get; private set; }

        public bool MainBreakerEnabled { get; private set; } = true;

        private BoundUserInterface _userInterface;

        private AppearanceComponent _appearance;

        private const float HighPowerThreshold = 0.7f;

        private const float LowPowerThreshold = 0.3f;

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

        #region Apc Visuals & UI

        private ApcChargeState _lastChargeState;
        private bool _uiDirty = true;
        private float _lastCharge = 0f;
        private ApcExternalPowerState _lastExternalPowerState;

        private void UserInterfaceOnReceiveMessage(ServerBoundUserInterfaceMessage serverMsg)
        {
            if (serverMsg.Message is ApcToggleMainBreakerMessage)
            {
                MainBreakerEnabled = !MainBreakerEnabled;
                _uiDirty = true;
                EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/machines/machine_switch.ogg", Owner, AudioParams.Default.WithVolume(-2f));
            }
        }

        public void Update()
        {
            var newState = CalcChargeState();
            if (newState != _lastChargeState)
            {
                _lastChargeState = newState;
                _appearance.SetData(ApcVisuals.ChargeState, newState);
            }
            var newCharge = Battery.CurrentCharge;
            if (newCharge != _lastCharge)
            {
                _lastCharge = newCharge;
                _uiDirty = true;
            }
            var extPowerState = CalcExtPowerState();
            if (extPowerState != _lastExternalPowerState)
            {
                _lastExternalPowerState = extPowerState;
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
            if (chargeFraction < LowPowerThreshold)
            {
                return ApcChargeState.Lack;
            }
            else if (chargeFraction > HighPowerThreshold)
            {
                return ApcChargeState.Full;
            }
            else
            {
                return ApcChargeState.Charging;
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

        #endregion
    }
}
