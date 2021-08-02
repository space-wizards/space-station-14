using System;
using System.Runtime.CompilerServices;
using Content.Server.Alert;
using Content.Server.Pressure;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Prototypes;

namespace Content.Server.Atmos.Components
{
    /// <summary>
    ///     Barotrauma: injury because of changes in air pressure.
    /// </summary>
    [RegisterComponent]
    public class BarotraumaComponent : Component
    {
        public override string Name => "Barotrauma";

        // TODO PROTOTYPE Replace this datafield variable with prototype references, once they are supported.
        [Robust.Shared.IoC.Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [DataField("damageType")]
        private readonly string _damageTypeID = "Blunt";
        private DamageTypePrototype _damageType => _prototypeManager.Index<DamageTypePrototype>(_damageTypeID);

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
                // TODO QUESTION So this bit of code behaves such that 'lowPressureMultiplier' only ever  makes the
                // low pressure damage threshold lower. It cannot raise it. Is this intended behaviour? In other
                // words, should the if statement here be removed, and the case inequality above replaced with:
                // p <= Atmospherics.WarningLowPressure/lowPressureMultiplier
                case var p when p <= Atmospherics.WarningLowPressure:
                    pressure *= lowPressureMultiplier;
                    if (pressure > Atmospherics.WarningLowPressure)
                        goto default;

                    damageable.TryChangeDamage(_damageType, Atmospherics.LowPressureDamage, false, Owner);

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

                    damageable.TryChangeDamage(_damageType, damage, false, Owner);

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
