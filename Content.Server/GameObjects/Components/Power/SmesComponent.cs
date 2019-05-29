using System;
using Content.Server.GameObjects.Components.Sound;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Power;
using Content.Shared.Utility;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.Components.Power
{
    /// <summary>
    ///     Handles the "user-facing" side of the actual SMES object.
    ///     This is operations that are specific to the SMES, like UI and visuals.
    ///     Code interfacing with the powernet is handled in <see cref="PowerStorageComponent" />.
    /// </summary>
    public class SmesComponent : SharedSmesComponent, IActivate
    {
        PowerStorageComponent Storage;
        AppearanceComponent Appearance;
        PowerStorageNetComponent _storageNet;

        int LastChargeLevel = 0;
        ChargeState LastChargeState;

        private float _lastCharge = 0f;
        private SmesExternalPowerState _lastExternalPowerState;
        private BoundUserInterface _userInterface;
        private bool _uiDirty = true;

        public override void Initialize()
        {
            base.Initialize();
            Storage = Owner.GetComponent<PowerStorageComponent>();
            Appearance = Owner.GetComponent<AppearanceComponent>();

            _storageNet = Owner.GetComponent<PowerStorageNetComponent>();

            _userInterface = Owner.GetComponent<ServerUserInterfaceComponent>().GetBoundUserInterface(SmesUiKey.Key);
            _userInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
        }

        private void UserInterfaceOnOnReceiveMessage(BoundUserInterfaceMessage obj)
        {
            if (obj is SmesToggleMainBreakerMessage)
            {
                _storageNet.ChargePowernet = !_storageNet.ChargePowernet;
                _uiDirty = true;
                _clickSound();
            }
        }

        public void OnUpdate()
        {
            var newLevel = CalcChargeLevel();
            if (newLevel != LastChargeLevel)
            {
                LastChargeLevel = newLevel;
                Appearance.SetData(SmesVisuals.LastChargeLevel, newLevel);
            }

            var newState = Storage.GetChargeState();
            if (newState != LastChargeState)
            {
                LastChargeState = newState;
                Appearance.SetData(SmesVisuals.LastChargeState, newState);
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
                _userInterface.SetState(new SmesBoundInterfaceState(_storageNet.ChargePowernet, extPowerState,
                    newCharge / Storage.Capacity, _storageNet.ChargeRate, _storageNet.DistributionRate));
                _uiDirty = false;
            }
        }

        int CalcChargeLevel()
        {
            return ContentHelpers.RoundToLevels(Storage.Charge, Storage.Capacity, 6);
        }

        private SmesExternalPowerState CalcExtPowerState()
        {
            if (!Owner.TryGetComponent(out PowerNodeComponent node) || node.Parent == null)
            {
                return SmesExternalPowerState.None;
            }

            var net = node.Parent;
            if (net.LastTotalAvailable <= 0)
            {
                return SmesExternalPowerState.None;
            }

            return net.Lack > 0 ? SmesExternalPowerState.Low : SmesExternalPowerState.Good;
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent actor))
            {
                return;
            }

            _userInterface.Open(actor.playerSession);
        }

        private void _clickSound()
        {
            Owner.GetComponent<SoundComponent>().Play("/Audio/machines/machine_switch.ogg", AudioParams.Default.WithVolume(-2f));
        }
    }
}
