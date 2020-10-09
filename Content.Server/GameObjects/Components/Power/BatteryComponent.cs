using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power
{
    [RegisterComponent]
    public class BatteryComponent : Component
    {
        public override string Name => "Battery";

        [ViewVariables(VVAccess.ReadWrite)] public int MaxCharge { get => _maxCharge; set => SetMaxCharge(value); }
        private int _maxCharge;

        [ViewVariables(VVAccess.ReadWrite)]
        public float CurrentCharge { get => _currentCharge; set => SetCurrentCharge(value); }

        private float _currentCharge;

        [ViewVariables] public BatteryState BatteryState { get; private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _maxCharge, "maxCharge", 1000);
            serializer.DataField(ref _currentCharge, "startingCharge", 500);
        }

        public override void Initialize()
        {
            base.Initialize();
            UpdateStorageState();
        }

        /// <summary>
        ///     If sufficient charge is avaiable on the battery, use it. Otherwise, don't.
        /// </summary>
        public bool TryUseCharge(float chargeToUse)
        {
            if (chargeToUse >= CurrentCharge)
            {
                return false;
            }
            else
            {
                CurrentCharge -= chargeToUse;
                return true;
            }
        }

        public float UseCharge(float toDeduct)
        {
            var chargeChangedBy = Math.Min(CurrentCharge, toDeduct);
            CurrentCharge -= chargeChangedBy;
            return chargeChangedBy;
        }

        public void FillFrom(BatteryComponent battery)
        {
            var powerDeficit = MaxCharge - CurrentCharge;
            if (battery.TryUseCharge(powerDeficit))
            {
                CurrentCharge += powerDeficit;
            }
            else
            {
                CurrentCharge += battery.CurrentCharge;
                battery.CurrentCharge = 0;
            }
        }

        protected virtual void OnChargeChanged() { }

        private void UpdateStorageState()
        {
            if (CurrentCharge == MaxCharge)
            {
                BatteryState = BatteryState.Full;
            }
            else if (CurrentCharge == 0)
            {
                BatteryState = BatteryState.Empty;
            }
            else
            {
                BatteryState = BatteryState.PartlyFull;
            }
        }

        private void SetMaxCharge(int newMax)
        {
            _maxCharge = Math.Max(newMax, 0);
            _currentCharge = Math.Min(_currentCharge, MaxCharge);
            UpdateStorageState();
            OnChargeChanged();
        }

        private void SetCurrentCharge(float newChargeAmount)
        {
            _currentCharge = MathHelper.Clamp(newChargeAmount, 0, MaxCharge);
            UpdateStorageState();
            OnChargeChanged();
        }
    }

    public enum BatteryState
    {
        Full,
        PartlyFull,
        Empty
    }
}
