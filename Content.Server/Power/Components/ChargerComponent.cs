using Content.Shared.Containers.ItemSlots;
using Content.Shared.Power;

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
        public int ChargeRate = 20;

        [DataField("chargerSlot", required: true)]
        public ItemSlot ChargerSlot = new();

        private CellChargerStatus GetStatus()
        {
            if (!_entMan.TryGetComponent<TransformComponent>(Owner, out var xform) ||
                !xform.Anchored ||
                _entMan.TryGetComponent(Owner, out ApcPowerReceiverComponent? receiver) && !receiver.Powered)
            {
                return CellChargerStatus.Off;
            }

            if (!ChargerSlot.HasItem)
                return CellChargerStatus.Empty;

            if (HeldBattery != null && Math.Abs(HeldBattery.MaxCharge - HeldBattery.CurrentCharge) < 0.01)
                return CellChargerStatus.Charged;

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
                    receiver.Load = ChargeRate;
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
                return;

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
                return;

            HeldBattery.CurrentCharge += ChargeRate * frameTime;
            // Just so the sprite won't be set to 99.99999% visibility
            if (HeldBattery.MaxCharge - HeldBattery.CurrentCharge < 0.01)
            {
                HeldBattery.CurrentCharge = HeldBattery.MaxCharge;
            }

            UpdateStatus();
        }
    }
}
