using Content.Server.Administration.Logs;
using Content.Server.Atmos.Components;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Robust.Shared.Containers;

namespace Content.Server.Atmos.EntitySystems
{
    public sealed class BarotraumaSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly AlertsSystem _alertsSystem = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger= default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;

        private const float UpdateTimer = 1f;
        private float _timer;

        public override void Initialize()
        {
            SubscribeLocalEvent<PressureProtectionComponent, HighPressureEvent>(OnHighPressureEvent);
            SubscribeLocalEvent<PressureProtectionComponent, LowPressureEvent>(OnLowPressureEvent);

            SubscribeLocalEvent<PressureImmunityComponent, HighPressureEvent>(OnHighPressureImmuneEvent);
            SubscribeLocalEvent<PressureImmunityComponent, LowPressureEvent>(OnLowPressureImmuneEvent);

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


        /// <summary>
        /// Completely prevent high pressure damage
        /// </summary>
        private void OnHighPressureImmuneEvent(EntityUid uid, PressureImmunityComponent component, HighPressureEvent args)
        {
            args.Multiplier = 0;
        }

        /// <summary>
        /// Completely prevent low pressure damage
        /// </summary>
        private void OnLowPressureImmuneEvent(EntityUid uid, PressureImmunityComponent component, LowPressureEvent args)
        {
            args.Modifier = 100;
            args.Multiplier = 10000;
        }

        public float GetFeltLowPressure(BarotraumaComponent baro, float environmentPressure)
        {
            var modifier = float.MaxValue;
            var multiplier = float.MaxValue;

            TryComp(baro.Owner, out InventoryComponent? inv);
            TryComp(baro.Owner, out ContainerManagerComponent? contMan);

            // TODO: cache this & update when equipment changes?
            // This continuously raises events for every player in space.

            // First, check if for protective equipment
            foreach (var slot in baro.ProtectionSlots)
            {
                if (!_inventorySystem.TryGetSlotEntity(baro.Owner, slot, out var equipment, inv, contMan)
                    || ! TryComp(equipment, out PressureProtectionComponent? protection))
                {
                    // Missing protection, skin is exposed.
                    modifier = 0;
                    multiplier = 1;
                    break;
                }

                modifier = Math.Min(protection.LowPressureModifier, modifier);
                multiplier = Math.Min(protection.LowPressureMultiplier, multiplier);
            }

            // Then apply any generic, non-clothing related modifiers.
            var lowPressureEvent = new LowPressureEvent(environmentPressure);
            RaiseLocalEvent(baro.Owner, lowPressureEvent, false);

            return (environmentPressure + modifier + lowPressureEvent.Modifier) * (multiplier * lowPressureEvent.Multiplier);
        }

        public float GetFeltHighPressure(BarotraumaComponent baro, float environmentPressure)
        {
            var modifier = float.MinValue;
            var multiplier = float.MinValue;

            TryComp(baro.Owner, out InventoryComponent? inv);
            TryComp(baro.Owner, out ContainerManagerComponent? contMan);

            // TODO: cache this & update when equipment changes?
            // Not as import and as low-pressure, but probably still useful.

            // First, check if for protective equipment
            foreach (var slot in baro.ProtectionSlots)
            {
                if (!_inventorySystem.TryGetSlotEntity(baro.Owner, slot, out var equipment, inv, contMan)
                    || !TryComp(equipment, out PressureProtectionComponent? protection))
                {
                    // Missing protection, skin is exposed.
                    modifier = 0;
                    multiplier = 1;
                    break;
                }

                modifier = Math.Max(protection.LowPressureModifier, modifier);
                multiplier = Math.Max(protection.LowPressureMultiplier, multiplier);
            }

            // Then apply any generic, non-clothing related modifiers.
            var highPressureEvent = new HighPressureEvent(environmentPressure);
            RaiseLocalEvent(baro.Owner, highPressureEvent, false);

            return (environmentPressure + modifier + highPressureEvent.Modifier) * (multiplier * highPressureEvent.Multiplier);
        }

        public override void Update(float frameTime)
        {
            _timer += frameTime;

            if (_timer < UpdateTimer)
                return;

            _timer -= UpdateTimer;

            foreach (var (barotrauma, damageable, transform) in EntityManager.EntityQuery<BarotraumaComponent, DamageableComponent, TransformComponent>())
            {
                var uid = barotrauma.Owner;
                var totalDamage = FixedPoint2.Zero;
                foreach (var (barotraumaDamageType, _) in barotrauma.Damage.DamageDict)
                {
                    if (!damageable.Damage.DamageDict.TryGetValue(barotraumaDamageType, out var damage))
                        continue;
                    totalDamage += damage;
                }
                if (totalDamage >= barotrauma.MaxDamage)
                    continue;

                var pressure = 1f;

                if (_atmosphereSystem.GetContainingMixture(uid) is {} mixture)
                {
                    pressure = MathF.Max(mixture.Pressure, 1f);
                }

                switch (pressure)
                {
                    // Low pressure.
                    case <= Atmospherics.WarningLowPressure:
                        pressure = GetFeltLowPressure(barotrauma, pressure);

                        if (pressure > Atmospherics.WarningLowPressure)
                            goto default;

                        // Deal damage and ignore resistances. Resistance to pressure damage should be done via pressure protection gear.
                        _damageableSystem.TryChangeDamage(barotrauma.Owner, barotrauma.Damage * Atmospherics.LowPressureDamage, true, false);

                        if (!barotrauma.TakingDamage)
                        {
                            barotrauma.TakingDamage = true;
                            _adminLogger.Add(LogType.Barotrauma, $"{ToPrettyString(barotrauma.Owner):entity} started taking low pressure damage");
                        }

                        if (pressure <= Atmospherics.HazardLowPressure)
                        {
                            _alertsSystem.ShowAlert(barotrauma.Owner, AlertType.LowPressure, 2);
                            break;
                        }

                        _alertsSystem.ShowAlert(barotrauma.Owner, AlertType.LowPressure, 1);
                        break;

                    // High pressure.
                    case >= Atmospherics.WarningHighPressure:
                        pressure = GetFeltHighPressure(barotrauma, pressure);

                        if(pressure < Atmospherics.WarningHighPressure)
                            goto default;

                        var damageScale = MathF.Min((pressure / Atmospherics.HazardHighPressure) * Atmospherics.PressureDamageCoefficient, Atmospherics.MaxHighPressureDamage);

                        // Deal damage and ignore resistances. Resistance to pressure damage should be done via pressure protection gear.
                        _damageableSystem.TryChangeDamage(barotrauma.Owner, barotrauma.Damage * damageScale, true, false);

                        if (!barotrauma.TakingDamage)
                        {
                            barotrauma.TakingDamage = true;
                            _adminLogger.Add(LogType.Barotrauma, $"{ToPrettyString(barotrauma.Owner):entity} started taking high pressure damage");
                        }

                        if (pressure >= Atmospherics.HazardHighPressure)
                        {
                            _alertsSystem.ShowAlert(barotrauma.Owner, AlertType.HighPressure, 2);
                            break;
                        }

                        _alertsSystem.ShowAlert(barotrauma.Owner, AlertType.HighPressure, 1);
                        break;

                    // Normal pressure.
                    default:
                        if (barotrauma.TakingDamage)
                        {
                            barotrauma.TakingDamage = false;
                            _adminLogger.Add(LogType.Barotrauma, $"{ToPrettyString(barotrauma.Owner):entity} stopped taking pressure damage");
                        }
                        _alertsSystem.ClearAlertCategory(barotrauma.Owner, AlertCategory.Pressure);
                        break;
                }
            }
        }
    }
}
