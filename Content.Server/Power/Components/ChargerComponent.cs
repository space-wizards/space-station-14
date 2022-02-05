using System;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Power;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Power.Components
{
    [RegisterComponent]
    public sealed class ChargerComponent : Component
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        [ViewVariables]
        public BatteryComponent? HeldBattery;

        [ViewVariables]
        private CellChargerStatus _status;

        [DataField("chargeRate")]
        private int _chargeRate = 100;

        [DataField("transferEfficiency")]
        private float _transferEfficiency = 0.85f;

        [DataField("chargerSlot", required: true)]
        public ItemSlot ChargerSlot = new();

        private CellChargerStatus GetStatus()
        {
            if (_entMan.TryGetComponent(Owner, out ApcPowerReceiverComponent? receiver) &&
                !receiver.Powered)
            {
                return CellChargerStatus.Off;
            }
            if (!ChargerSlot.HasItem)
            {
                return CellChargerStatus.Empty;
            }
            if (HeldBattery != null && Math.Abs(HeldBattery.MaxCharge - HeldBattery.CurrentCharge) < 0.01)
            {
                return CellChargerStatus.Charged;
            }
            return CellChargerStatus.Charging;
        }

        public void UpdateStatus()
        {
            // Not called UpdateAppearance just because it messes with the load
            var status = GetStatus();
            if (_status == status ||
                !_entMan.TryGetComponent(Owner, out ApcPowerReceiverComponent? receiver))
            {
                return;
            }

            _status = status;
            _entMan.TryGetComponent(Owner, out AppearanceComponent? appearance);

            switch (_status)
            {
                // Update load just in case
                case CellChargerStatus.Off:
                    receiver.Load = 0;
                    appearance?.SetData(CellVisual.Light, CellChargerStatus.Off);
                    break;
                case CellChargerStatus.Empty:
                    receiver.Load = 0;
                    appearance?.SetData(CellVisual.Light, CellChargerStatus.Empty);
                    break;
                case CellChargerStatus.Charging:
                    receiver.Load = (int) (_chargeRate / _transferEfficiency);
                    appearance?.SetData(CellVisual.Light, CellChargerStatus.Charging);
                    break;
                case CellChargerStatus.Charged:
                    receiver.Load = 0;
                    appearance?.SetData(CellVisual.Light, CellChargerStatus.Charged);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            appearance?.SetData(CellVisual.Occupied, ChargerSlot.HasItem);
        }

        public void OnUpdate(float frameTime) //todo: make single system for this
        {
            if (_status == CellChargerStatus.Empty || _status == CellChargerStatus.Charged || !ChargerSlot.HasItem)
            {
                return;
            }
            TransferPower(frameTime);
        }

        private void TransferPower(float frameTime)
        {
            if (_entMan.TryGetComponent(Owner, out ApcPowerReceiverComponent? receiver) &&
                !receiver.Powered)
            {
                return;
            }

            if (HeldBattery == null)
            {
                return;
            }

            HeldBattery.CurrentCharge += _chargeRate * frameTime;
            // Just so the sprite won't be set to 99.99999% visibility
            if (HeldBattery.MaxCharge - HeldBattery.CurrentCharge < 0.01)
            {
                HeldBattery.CurrentCharge = HeldBattery.MaxCharge;
            }
            UpdateStatus();
        }
    }
}
