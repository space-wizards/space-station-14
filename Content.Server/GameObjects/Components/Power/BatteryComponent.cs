using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;

namespace Content.Server.GameObjects.Components.Power
{
    [RegisterComponent]
    public class BatteryComponent : Component
    {
        public override string Name => "Battery";

        [ViewVariables(VVAccess.ReadWrite)]
        public int MaxCharge { get => _maxCharge; set => SetMaxCharge(value); }
        private int _maxCharge;

        [ViewVariables(VVAccess.ReadWrite)]
        public float CurrentCharge { get => _currentCharge; set => SetCurrentCharge(value); }
        private float _currentCharge;

        /// <summary>
        ///     How full the battery is.
        /// </summary>
        [ViewVariables]
        public BatteryState BatteryState { get; private set; }

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
            if (chargeToUse > CurrentCharge)
            {
                return false;
            }
            else
            {
                CurrentCharge -= chargeToUse;
                return true;
            }
        }

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
            _currentCharge = Math.Min( _currentCharge, MaxCharge);
            UpdateStorageState();
        }

        private void SetCurrentCharge(float newChargeAmount)
        {
            var oldCharge = _currentCharge;
            _currentCharge = FloatMath.Clamp(newChargeAmount, 0, MaxCharge);
            UpdateStorageState();
        }

        //Temp refactor trash
        private float _wattage = 1;
        public float RequestCharge(float frameTime)
        {
            return Math.Min(_wattage * frameTime, MaxCharge - CurrentCharge);
        }
        public float AvailableCharge(float frameTime)
        {
            return Math.Min(_wattage * frameTime, CurrentCharge);
        }
        public bool TryDeductWattage(float wattage, float frameTime)
        {
            var avail = AvailableCharge(frameTime);
            if (avail < wattage * frameTime)
            {
                return false;
            }
            CurrentCharge -= wattage * frameTime;
            return true;
        }
    }

    public enum BatteryState
    {
        Full,
        PartlyFull,
        Empty
    }
}
