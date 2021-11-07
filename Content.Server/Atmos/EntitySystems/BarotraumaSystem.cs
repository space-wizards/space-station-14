using System;
using System.Data;
using Content.Server.Alert;
using Content.Server.Atmos.Components;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Atmos.EntitySystems
{
    public class BarotraumaSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;

        private const float UpdateTimer = 1f;

        private float _timer = 0f;

        public override void Initialize()
        {
            SubscribeLocalEvent<PressureProtectionComponent, HighPressureEvent>(OnHighPressureEvent);
            SubscribeLocalEvent<PressureProtectionComponent, LowPressureEvent>(OnLowPressureEvent);
        }

        private void OnHighPressureEvent(EntityUid uid, PressureProtectionComponent component, HighPressureEvent args)
        {
            args.Modifier += component.HighPressureModifier;
            args.Multiplier *= component.HighPressureMultiplier;
        }

        private void OnLowPressureEvent(EntityUid uid, PressureProtectionComponent component, LowPressureEvent args)
        {
            args.Modifier += component.LowPressureModifier;
            args.Multiplier *= component.LowPressureMultiplier;
        }

        private float CalculateFeltPressure(float environmentPressure, PressureEvent pressureEvent)
        {
            environmentPressure += pressureEvent.Modifier;
            environmentPressure *= pressureEvent.Multiplier;
            return environmentPressure;
        }

        public float GetFeltLowPressure(EntityUid uid, float environmentPressure)
        {
            var lowPressureEvent = new LowPressureEvent(environmentPressure);
            RaiseLocalEvent(uid, lowPressureEvent, false);

            return CalculateFeltPressure(environmentPressure, lowPressureEvent);
        }

        public float GetFeltHighPressure(EntityUid uid, float environmentPressure)
        {
            var highPressureEvent = new HighPressureEvent(environmentPressure);
            RaiseLocalEvent(uid, highPressureEvent, false);

            return CalculateFeltPressure(environmentPressure, highPressureEvent);
        }

        public override void Update(float frameTime)
        {
            _timer += frameTime;

            if (_timer < UpdateTimer)
                return;

            _timer -= UpdateTimer;

            foreach (var (barotrauma, damageable, transform) in EntityManager.EntityQuery<BarotraumaComponent, DamageableComponent, ITransformComponent>(false))
            {
                var totalDamage = FixedPoint2.Zero;
                foreach (var (barotraumaDamageType, _) in barotrauma.Damage.DamageDict)
                {
                    if (!damageable.Damage.DamageDict.TryGetValue(barotraumaDamageType, out var damage))
                        continue;
                    totalDamage += damage;
                }
                if (totalDamage >= barotrauma.MaxDamage)
                    continue;

                var uid = barotrauma.Owner.Uid;

                var status = barotrauma.Owner.GetComponentOrNull<ServerAlertsComponent>();

                var pressure = 1f;

                if (_atmosphereSystem.GetTileMixture(transform.Coordinates) is { } mixture)
                {
                    pressure = MathF.Max(mixture.Pressure, 1f);;
                }

                switch (pressure)
                {
                    // Low pressure.
                    case <= Atmospherics.WarningLowPressure:
                        pressure = GetFeltLowPressure(uid, pressure);

                        if (pressure > Atmospherics.WarningLowPressure)
                            goto default;

                        // Deal damage and ignore resistances. Resistance to pressure damage should be done via pressure protection gear.
                        _damageableSystem.TryChangeDamage(uid, barotrauma.Damage * Atmospherics.LowPressureDamage, true);

                        if (status == null) break;

                        if (pressure <= Atmospherics.HazardLowPressure)
                        {
                            status.ShowAlert(AlertType.LowPressure, 2);
                            break;
                        }

                        status.ShowAlert(AlertType.LowPressure, 1);
                        break;

                    // High pressure.
                    case >= Atmospherics.WarningHighPressure:
                        pressure = GetFeltHighPressure(uid, pressure);

                        if(pressure < Atmospherics.WarningHighPressure)
                            goto default;

                        var damageScale = (int) MathF.Min((pressure / Atmospherics.HazardHighPressure) * Atmospherics.PressureDamageCoefficient, Atmospherics.MaxHighPressureDamage);

                        // Deal damage and ignore resistances. Resistance to pressure damage should be done via pressure protection gear.
                        _damageableSystem.TryChangeDamage(uid, barotrauma.Damage * damageScale, true);

                        if (status == null) break;

                        if (pressure >= Atmospherics.HazardHighPressure)
                        {
                            status.ShowAlert(AlertType.HighPressure, 2);
                            break;
                        }

                        status.ShowAlert(AlertType.HighPressure, 1);
                        break;

                    // Normal pressure.
                    default:
                        status?.ClearAlertCategory(AlertCategory.Pressure);
                        break;
                }
            }
        }
    }
}
