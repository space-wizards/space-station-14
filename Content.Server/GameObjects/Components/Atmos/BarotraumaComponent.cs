using System;
using System.Runtime.CompilerServices;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Atmos
{
    /// <summary>
    ///     Barotrauma: injury because of changes in air pressure.
    /// </summary>
    [RegisterComponent]
    public class BarotraumaComponent : Component
    {
        public override string Name => "Barotrauma";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(float airPressure)
        {
            if (!Owner.TryGetComponent(out IDamageableComponent damageable)) return;
            Owner.TryGetComponent(out ServerStatusEffectsComponent status);

            var highPressureMultiplier = 1f;
            var lowPressureMultiplier = 1f;

            foreach (var protection in Owner.GetAllComponents<IPressureProtection>())
            {
                highPressureMultiplier *= protection.HighPressureMultiplier;
                lowPressureMultiplier *= protection.LowPressureMultiplier;
            }

            var pressure = MathF.Max(airPressure, 1f);

            switch (pressure)
            {
                // Low pressure.
                case var p when p <= Atmospherics.WarningLowPressure:
                    pressure *= lowPressureMultiplier;

                    if(pressure > Atmospherics.WarningLowPressure)
                        goto default;

                    damageable.ChangeDamage(DamageType.Blunt, Atmospherics.LowPressureDamage, false, Owner);

                    if (status == null) break;

                    if (pressure <= Atmospherics.HazardLowPressure)
                    {
                        status.ChangeStatusEffect(StatusEffect.Pressure, "/Textures/Interface/StatusEffects/Pressure/lowpressure2.png", null);
                        break;
                    }

                    status.ChangeStatusEffect(StatusEffect.Pressure, "/Textures/Interface/StatusEffects/Pressure/lowpressure1.png", null);
                    break;

                // High pressure.
                case var p when p >= Atmospherics.WarningHighPressure:
                    pressure *= highPressureMultiplier;

                    if(pressure < Atmospherics.WarningHighPressure)
                        goto default;

                    var damage = (int) MathF.Min((pressure / Atmospherics.HazardHighPressure) * Atmospherics.PressureDamageCoefficient, Atmospherics.MaxHighPressureDamage);

                    damageable.ChangeDamage(DamageType.Blunt, damage, false, Owner);

                    if (status == null) break;

                    if (pressure >= Atmospherics.HazardHighPressure)
                    {
                        status.ChangeStatusEffect(StatusEffect.Pressure, "/Textures/Interface/StatusEffects/Pressure/highpressure2.png", null);
                        break;
                    }

                    status.ChangeStatusEffect(StatusEffect.Pressure, "/Textures/Interface/StatusEffects/Pressure/highpressure1.png", null);
                    break;

                // Normal pressure.
                default:
                    status?.RemoveStatusEffect(StatusEffect.Pressure);
                    break;
            }

        }
    }
}
