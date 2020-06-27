using Content.Server.GameObjects.Components.Power.PowerNetComponents;
using Content.Shared.GameObjects.Components.Power;
using Content.Shared.Utility;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using System;

namespace Content.Server.GameObjects.Components.Power
{
    /// <summary>
    ///     Handles the "user-facing" side of the actual SMES object.
    ///     This is operations that are specific to the SMES, like UI and visuals.
    ///     Code interfacing with the powernet is handled in <see cref="BatteryStorageComponent"/> and <see cref="BatteryDischargerComponent"/>.
    /// </summary>
    [RegisterComponent]
    public class SmesComponent : Component
    {
        public override string Name => "Smes";

        private BatteryComponent _battery;

        private AppearanceComponent _appearance;

        private int _lastChargeLevel = 0;

        private TimeSpan _lastChargeLevelChange;

        private ChargeState _lastChargeState;

        private TimeSpan _lastChargeStateChange;

        private const int VisualsChangeDelay = 1;

#pragma warning disable 649
        [Dependency] private readonly IGameTiming _gameTiming;
#pragma warning restore 649

        public override void Initialize()
        {
            base.Initialize();
            _battery = Owner.GetComponent<BatteryComponent>();
            _appearance = Owner.GetComponent<AppearanceComponent>();
        }

        public void OnUpdate()
        {
            var newLevel = GetNewChargeLevel();
            if (newLevel != _lastChargeLevel && _lastChargeLevelChange + TimeSpan.FromSeconds(VisualsChangeDelay) < _gameTiming.CurTime)
            {
                _lastChargeLevel = newLevel;
                _lastChargeLevelChange = _gameTiming.CurTime;
                _appearance.SetData(SmesVisuals.LastChargeLevel, newLevel);
            }

            var newChargeState = GetNewChargeState();
            if (newChargeState != _lastChargeState && _lastChargeStateChange + TimeSpan.FromSeconds(VisualsChangeDelay) < _gameTiming.CurTime)
            {
                _lastChargeState = newChargeState;
                _lastChargeStateChange = _gameTiming.CurTime;
                _appearance.SetData(SmesVisuals.LastChargeState, newChargeState);
            }            
        }

        private int GetNewChargeLevel()
        {
            return ContentHelpers.RoundToLevels(_battery.CurrentCharge, _battery.MaxCharge, 6);
        }

        private ChargeState GetNewChargeState()
        {
            var supplier = Owner.GetComponent<PowerSupplierComponent>();
            var consumer = Owner.GetComponent<PowerConsumerComponent>();
            if (supplier.SupplyRate > 0 && consumer.DrawRate != consumer.ReceivedPower)
            {
                return ChargeState.Discharging;
            }
            else if (supplier.SupplyRate == 0 && consumer.DrawRate > 0)
            {
                return ChargeState.Charging;
            }
            else
            {
                return ChargeState.Still;
            }
        }
    }
}
