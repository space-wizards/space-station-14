using Content.Server.Power.Components;
using Content.Shared.Power;
using Content.Shared.Rounding;
using Content.Shared.SMES;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.Power.SMES
{
    /// <summary>
    ///     Handles the "user-facing" side of the actual SMES object.
    ///     This is operations that are specific to the SMES, like UI and visuals.
    ///     Code interfacing with the powernet is handled in <see cref="BatteryStorageComponent"/> and <see cref="BatteryDischargerComponent"/>.
    /// </summary>
    [RegisterComponent]
    public sealed class SmesComponent : Component
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private int _lastChargeLevel;

        private TimeSpan _lastChargeLevelChange;

        private ChargeState _lastChargeState;

        private TimeSpan _lastChargeStateChange;

        private const int VisualsChangeDelay = 1;

        protected override void Initialize()
        {
            base.Initialize();

            Owner.EnsureComponentWarn<ServerAppearanceComponent>();
        }

        public void OnUpdate()
        {
            var newLevel = GetNewChargeLevel();
            if (newLevel != _lastChargeLevel && _lastChargeLevelChange + TimeSpan.FromSeconds(VisualsChangeDelay) < _gameTiming.CurTime)
            {
                _lastChargeLevel = newLevel;
                _lastChargeLevelChange = _gameTiming.CurTime;

                if (_entMan.TryGetComponent(Owner, out AppearanceComponent? appearance))
                {
                    appearance.SetData(SmesVisuals.LastChargeLevel, newLevel);
                }
            }

            var newChargeState = GetNewChargeState();
            if (newChargeState != _lastChargeState && _lastChargeStateChange + TimeSpan.FromSeconds(VisualsChangeDelay) < _gameTiming.CurTime)
            {
                _lastChargeState = newChargeState;
                _lastChargeStateChange = _gameTiming.CurTime;

                if (_entMan.TryGetComponent(Owner, out AppearanceComponent? appearance))
                {
                    appearance.SetData(SmesVisuals.LastChargeState, newChargeState);
                }
            }
        }

        private int GetNewChargeLevel()
        {
            if (!_entMan.TryGetComponent(Owner, out BatteryComponent? battery))
            {
                return 0;
            }

            return ContentHelpers.RoundToLevels(battery.CurrentCharge, battery.MaxCharge, 6);
        }

        private ChargeState GetNewChargeState()
        {
            var battery = _entMan.GetComponent<PowerNetworkBatteryComponent>(Owner);
            return (battery.CurrentSupply - battery.CurrentReceiving) switch
            {
                > 0 => ChargeState.Discharging,
                < 0 => ChargeState.Charging,
                _ => ChargeState.Still
            };
        }
    }
}
