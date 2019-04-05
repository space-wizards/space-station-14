using Content.Server.GameObjects.Components.Sound;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Power;
using SS14.Server.GameObjects;
using SS14.Server.GameObjects.Components.UserInterface;
using SS14.Server.GameObjects.EntitySystems;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.Audio;
using SS14.Shared.GameObjects.Components.UserInterface;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;

namespace Content.Server.GameObjects.Components.Power
{
    public sealed class ApcComponent : SharedApcComponent, IAttackHand
    {
        PowerStorageComponent Storage;
        AppearanceComponent Appearance;
        private PowerProviderComponent _provider;

        ApcChargeState LastChargeState;
        private float _lastCharge = 0f;
        private ApcExternalPowerState _lastExternalPowerState;
        private BoundUserInterface _userInterface;
        private bool _uiDirty = true;

        public override void Initialize()
        {
            base.Initialize();
            Storage = Owner.GetComponent<PowerStorageComponent>();
            Appearance = Owner.GetComponent<AppearanceComponent>();
            _provider = Owner.GetComponent<PowerProviderComponent>();
            _userInterface = Owner.GetComponent<ServerUserInterfaceComponent>().GetBoundUserInterface(ApcUiKey.Key);
            _userInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
        }

        private void UserInterfaceOnOnReceiveMessage(BoundUserInterfaceMessage obj)
        {
            if (obj is ApcToggleMainBreakerMessage)
            {
                _provider.MainBreaker = !_provider.MainBreaker;
                _uiDirty = true;
                _clickSound();
            }
        }

        public void OnUpdate()
        {
            var newState = CalcChargeState();
            if (newState != LastChargeState)
            {
                LastChargeState = newState;
                Appearance.SetData(ApcVisuals.ChargeState, newState);
            }

            var newCharge = Storage.Charge;
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
                _userInterface.SetState(new ApcBoundInterfaceState(_provider.MainBreaker, extPowerState,
                    newCharge / Storage.Capacity));
                _uiDirty = false;
            }
        }

        private ApcChargeState CalcChargeState()
        {
            var storageCharge = Storage.GetChargeState();
            switch (storageCharge)
            {
                case ChargeState.Discharging:
                    return ApcChargeState.Lack;
                case ChargeState.Charging:
                    return ApcChargeState.Charging;
                default:
                    // Still.
                    return Storage.Full ? ApcChargeState.Full : ApcChargeState.Lack;
            }
        }

        private ApcExternalPowerState CalcExtPowerState()
        {
            if (!Owner.TryGetComponent(out PowerNodeComponent node) || node.Parent == null)
            {
                return ApcExternalPowerState.None;
            }

            var net = node.Parent;
            if (net.LastTotalAvailable <= 0)
            {
                return ApcExternalPowerState.None;
            }

            return net.Lack > 0 ? ApcExternalPowerState.Low : ApcExternalPowerState.Good;
        }

        bool IAttackHand.AttackHand(AttackHandEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent actor))
            {
                return false;
            }

            _userInterface.Open(actor.playerSession);
            return true;
        }

        private void _clickSound()
        {
            Owner.GetComponent<SoundComponent>().Play("/Audio/machines/machine_switch.ogg", AudioParams.Default.WithVolume(-2f));
        }
    }
}
