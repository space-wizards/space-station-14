#nullable enable
using System;
using Content.Shared.GameObjects.Components.Power;
using Content.Shared.Utility;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.Components.Power.PowerNetComponents
{
    /// <summary>
    ///     Handles the "user-facing" side of the actual SMES object.
    ///     This is operations that are specific to the SMES, like UI and visuals.
    ///     Code interfacing with the powernet is handled in <see cref="BatteryStorageComponent"/> and <see cref="BatteryDischargerComponent"/>.
    /// </summary>
    [RegisterComponent]
    public class SmesComponent : Component
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override string Name => "Smes";

        private int _lastChargeLevel;

        private TimeSpan _lastChargeLevelChange;

        private ChargeState _lastChargeState;

        private TimeSpan _lastChargeStateChange;

        private const int VisualsChangeDelay = 1;

        public override void Initialize()
        {
            base.Initialize();

            Owner.EnsureComponent<BatteryComponent>();
            Owner.EnsureComponent<AppearanceComponent>();
        }

        public void OnUpdate()
        {
            var newLevel = GetNewChargeLevel();
            if (newLevel != _lastChargeLevel && _lastChargeLevelChange + TimeSpan.FromSeconds(VisualsChangeDelay) < _gameTiming.CurTime)
            {
                _lastChargeLevel = newLevel;
                _lastChargeLevelChange = _gameTiming.CurTime;

                if (Owner.TryGetComponent(out AppearanceComponent? appearance))
                {
                    appearance.SetData(SmesVisuals.LastChargeLevel, newLevel);
                }
            }

            var newChargeState = GetNewChargeState();
            if (newChargeState != _lastChargeState && _lastChargeStateChange + TimeSpan.FromSeconds(VisualsChangeDelay) < _gameTiming.CurTime)
            {
                _lastChargeState = newChargeState;
                _lastChargeStateChange = _gameTiming.CurTime;

                if (Owner.TryGetComponent(out AppearanceComponent? appearance))
                {
                    appearance.SetData(SmesVisuals.LastChargeState, newChargeState);
                }
            }
        }

        private int GetNewChargeLevel()
        {
            if (!Owner.TryGetComponent(out BatteryComponent? battery))
            {
                return 0;
            }

            return ContentHelpers.RoundToLevels(battery.CurrentCharge, battery.MaxCharge, 6);
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
