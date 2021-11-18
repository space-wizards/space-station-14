using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Alert;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Temperature.Components;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Temperature.Systems
{
    public class TemperatureSystem : EntitySystem
    {
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

        /// <summary>
        ///     All the components that will have their damage updated at the end of the tick.
        ///     This is done because both AtmosExposed and Flammable call ReceiveHeat in the same tick, meaning
        ///     that we need some mechanism to ensure it doesn't double dip on damage for both calls.
        /// </summary>
        public HashSet<TemperatureComponent> ShouldUpdateDamage = new();

        public float UpdateInterval = 1.0f;

        private float _accumulatedFrametime = 0.0f;

        public override void Initialize()
        {
            SubscribeLocalEvent<TemperatureComponent, OnTemperatureChangeEvent>(EnqueueDamage);
            SubscribeLocalEvent<TemperatureComponent, AtmosExposedUpdateEvent>(OnAtmosExposedUpdate);
            SubscribeLocalEvent<ServerAlertsComponent, OnTemperatureChangeEvent>(ServerAlert);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            _accumulatedFrametime += frameTime;

            if (_accumulatedFrametime < UpdateInterval)
                return;
            _accumulatedFrametime -= UpdateInterval;

            if (!ShouldUpdateDamage.Any())
                return;

            foreach (var comp in ShouldUpdateDamage)
            {
                if (comp.Deleted || comp.Paused)
                    continue;

                ChangeDamage(comp.OwnerUid, comp);
            }

            ShouldUpdateDamage.Clear();
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

        private void EnqueueDamage(EntityUid uid, TemperatureComponent component, OnTemperatureChangeEvent args)
        {
            ShouldUpdateDamage.Add(component);
        }

        private void ChangeDamage(EntityUid uid, TemperatureComponent temperature)
        {
            if (!EntityManager.TryGetComponent<DamageableComponent>(uid, out var damage))
                return;

            if (temperature.CurrentTemperature >= temperature.HeatDamageThreshold)
            {
                var tempDamage = FixedPoint2.Min(temperature.CurrentTemperature - temperature.HeatDamageThreshold * temperature.TempDamageCoefficient, temperature.DamageCap);
                _damageableSystem.TryChangeDamage(uid, temperature.HeatDamage * tempDamage);
            }
            else if (temperature.CurrentTemperature <= temperature.ColdDamageThreshold)
            {
                var tempDamage = FixedPoint2.Min(temperature.CurrentTemperature - temperature.ColdDamageThreshold * temperature.TempDamageCoefficient, temperature.DamageCap);
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
