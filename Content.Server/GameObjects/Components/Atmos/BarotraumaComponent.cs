using System;
using System.Runtime.CompilerServices;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Dependency = Robust.Shared.IoC.DependencyAttribute;

namespace Content.Server.GameObjects.Components.Atmos
{
    /// <summary>
    ///     Barotrauma: injury because of changes in air pressure.
    /// </summary>
    [RegisterComponent]
    public class BarotraumaComponent : Component
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override string Name => "Barotrauma";

        [DataField("damageType",required: true)]
        private readonly DamageTypePrototype _damageType = default!;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(float airPressure)
        {
            if (!Owner.TryGetComponent(out IDamageableComponent? damageable)) return;

            var status = Owner.GetComponentOrNull<ServerAlertsComponent>();
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

                    damageable.ChangeDamage(damageable.GetDamageType("Blunt"), Atmospherics.LowPressureDamage, false, Owner);

                    if (status == null) break;

                    if (pressure <= Atmospherics.HazardLowPressure)
                    {
                        status.ShowAlert(AlertType.LowPressure, 2);
                        break;
                    }

                    status.ShowAlert(AlertType.LowPressure, 1);
                    break;

                // High pressure.
                case var p when p >= Atmospherics.WarningHighPressure:
                    pressure *= highPressureMultiplier;

                    if(pressure < Atmospherics.WarningHighPressure)
                        goto default;

                    var damage = (int) MathF.Min((pressure / Atmospherics.HazardHighPressure) * Atmospherics.PressureDamageCoefficient, Atmospherics.MaxHighPressureDamage);

                    damageable.ChangeDamage(_damageType, damage, false, Owner);

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
