using System;
using System.Runtime.CompilerServices;
using Content.Server.Alert;
using Content.Server.Pressure;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Robust.Shared.GameObjects;
<<<<<<< refs/remotes/origin/master
=======
using Robust.Shared.Serialization.Manager.Attributes;
using Dependency = Robust.Shared.IoC.DependencyAttribute;
>>>>>>> update damagecomponent across shared and server

namespace Content.Server.Atmos.Components
{
    /// <summary>
    ///     Barotrauma: injury because of changes in air pressure.
    /// </summary>
    [RegisterComponent]
    public class BarotraumaComponent : Component
    {
        public override string Name => "Barotrauma";

<<<<<<< refs/remotes/origin/master
=======
        [DataField("damageType", required: true)]
        private readonly string _damageType = default!;

>>>>>>> update damagecomponent across shared and server
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

                    damageable.ChangeDamage(DamageType.Blunt, Atmospherics.LowPressureDamage, false, Owner);

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

<<<<<<< refs/remotes/origin/master
                    damageable.ChangeDamage(DamageType.Blunt, damage, false, Owner);
=======
                    damageable.ChangeDamage(damageable.GetDamageType(_damageType), damage, false, Owner);
>>>>>>> update damagecomponent across shared and server

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
