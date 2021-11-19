using System;
using Content.Server.Alert;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Temperature.Components;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Temperature.Systems
{
    public class TemperatureSystem : EntitySystem
    {
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<TemperatureComponent, OnTemperatureChangeEvent>(ChangeDamage);
            SubscribeLocalEvent<TemperatureComponent, AtmosExposedUpdateEvent>(OnAtmosExposedUpdate);
            SubscribeLocalEvent<ServerAlertsComponent, OnTemperatureChangeEvent>(ServerAlert);
        }

        public void ForceChangeTemperature(EntityUid uid, float temp, TemperatureComponent? temperature = null)
        {
            if (Resolve(uid, ref temperature))
            {
                float lastTemp = temperature.CurrentTemperature;
                float delta = temperature.CurrentTemperature - temp;
                temperature.CurrentTemperature = temp;
                RaiseLocalEvent(uid, new OnTemperatureChangeEvent(temperature.CurrentTemperature, lastTemp, delta));
            }
        }

        public void ReceiveHeat(EntityUid uid, float heatAmount, TemperatureComponent? temperature = null)
        {
            if (Resolve(uid, ref temperature))
            {
                float lastTemp = temperature.CurrentTemperature;
                temperature.CurrentTemperature += heatAmount / temperature.HeatCapacity;
                float delta = temperature.CurrentTemperature - lastTemp;

                RaiseLocalEvent(uid, new OnTemperatureChangeEvent(temperature.CurrentTemperature, lastTemp, delta));
            }
        }

        public void RemoveHeat(EntityUid uid, float heatAmount, TemperatureComponent? temperature = null)
        {
            if (Resolve(uid, ref temperature))
            {
                float lastTemp = temperature.CurrentTemperature;
                temperature.CurrentTemperature -= heatAmount / temperature.HeatCapacity;
                float delta = temperature.CurrentTemperature - lastTemp;

                RaiseLocalEvent(uid, new OnTemperatureChangeEvent(temperature.CurrentTemperature, lastTemp, delta));
            }
        }

        private void OnAtmosExposedUpdate(EntityUid uid, TemperatureComponent temperature, ref AtmosExposedUpdateEvent args)
        {
            var temperatureDelta = args.GasMixture.Temperature - temperature.CurrentTemperature;
            var tileHeatCapacity = _atmosphereSystem.GetHeatCapacity(args.GasMixture);
            var heat = temperatureDelta * (tileHeatCapacity * temperature.HeatCapacity / (tileHeatCapacity + temperature.HeatCapacity));
            ReceiveHeat(uid, heat, temperature);
        }

        private void ServerAlert(EntityUid uid, ServerAlertsComponent status, OnTemperatureChangeEvent args)
        {
            switch (args.CurrentTemperature)
            {
                // Cold strong.
                case <= 260:
                    status.ShowAlert(AlertType.Cold, 3);
                    break;

                // Cold mild.
                case <= 280 and > 260:
                    status.ShowAlert(AlertType.Cold, 2);
                    break;

                // Cold weak.
                case <= 292 and > 280:
                    status.ShowAlert(AlertType.Cold, 1);
                    break;

                // Safe.
                case <= 327 and > 292:
                    status.ClearAlertCategory(AlertCategory.Temperature);
                    break;

                // Heat weak.
                case <= 335 and > 327:
                    status.ShowAlert(AlertType.Hot, 1);
                    break;

                // Heat mild.
                case <= 345 and > 335:
                    status.ShowAlert(AlertType.Hot, 2);
                    break;

                // Heat strong.
                case > 345:
                    status.ShowAlert(AlertType.Hot, 3);
                    break;
            }
        }

        private void ChangeDamage(EntityUid uid, TemperatureComponent temperature, OnTemperatureChangeEvent args)
        {
            if (!EntityManager.TryGetComponent<DamageableComponent>(uid, out var damage))
                return;

            if (args.CurrentTemperature >= temperature.HeatDamageThreshold)
            {
                int tempDamage = (int) Math.Floor((args.CurrentTemperature - temperature.HeatDamageThreshold) * temperature.TempDamageCoefficient);
                _damageableSystem.TryChangeDamage(uid, temperature.HeatDamage * tempDamage);
            }
            else if (args.CurrentTemperature <= temperature.ColdDamageThreshold)
            {
                int tempDamage = (int) Math.Floor((temperature.ColdDamageThreshold - args.CurrentTemperature) * temperature.TempDamageCoefficient);
                _damageableSystem.TryChangeDamage(uid, temperature.ColdDamage * tempDamage);
            }

        }
    }

    public class OnTemperatureChangeEvent : EntityEventArgs
    {
        public float CurrentTemperature { get; }
        public float LastTemperature { get; }
        public float TemperatureDelta { get; }

        public OnTemperatureChangeEvent(float current, float last, float delta)
        {
            CurrentTemperature = current;
            LastTemperature = last;
            TemperatureDelta = delta;
        }
    }
}
