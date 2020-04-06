using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;

namespace Content.Server.GameObjects.Components.NewPower
{
    /// <summary>
    ///     Represent a battery. Can have charge added up to a max, and can have its charge removed.
    /// </summary>
    [RegisterComponent]
    public class BatteryComponent : Component
    {
        public override string Name => "Battery";

        /// <summary>
        ///     The max amount of charge (Joules) this battery can hold.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int MaxCharge { get => _maxCharge; set => SetMaxCharge(value); }
        private int _maxCharge;

        /// <summary>
        ///     How much charge (Joules) this battery is holding.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float CurrentCharge { get => _currentCharge; set => SetCurrentCharge(value); }
        private float _currentCharge;

        [ViewVariables]
        public BatteryState BatteryState { get; private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _maxCharge, "maxCharge", 1000);
            serializer.DataField(ref _currentCharge, "startingCharge", 500);
        }

        public override void OnAdd()
        {
            base.OnAdd();
            UpdateStorageState();
        }

        private void UpdateStorageState()
        {
            BatteryState newState;
            if (CurrentCharge == MaxCharge)
            {
                newState = BatteryState.Full;
            }
            else if (CurrentCharge == 0)
            {
                newState = BatteryState.Empty;
            }
            else
            {
                newState = BatteryState.PartlyFull;
            }
            if (newState != BatteryState)
            {
                BatteryState = newState;
            }
        }

        private void SetMaxCharge(int newMax)
        {
            _maxCharge = Math.Max(newMax, 0); //cannot go below 0
            if (CurrentCharge > MaxCharge)
            {
                CurrentCharge = MaxCharge;
            }
            UpdateStorageState();
        }

        private void SetCurrentCharge(float chargeAmount)
        {
            _currentCharge = FloatMath.Clamp(chargeAmount, 0, MaxCharge); //cannot go below 0 or above the battery's max charge
            UpdateStorageState();
        }
    }

    public enum BatteryState
    {
        Full,
        PartlyFull,
        Empty
    }
}
