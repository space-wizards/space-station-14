using System;
using System.Runtime.CompilerServices;
using Content.Server.Alert;
using Content.Server.Pressure;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Components
{
    /// <summary>
    ///     Barotrauma: injury because of changes in air pressure.
    /// </summary>
    [RegisterComponent]
    public class BarotraumaComponent : Component
    {
        public override string Name => "Barotrauma";

        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(float airPressure)
        {
            if (!Owner.HasComponent<DamageableComponent>()) return;

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
                    if (pressure > Atmospherics.WarningLowPressure)
                        goto default;

                    // Deal damage and ignore resistances. Resistance to pressure damage should be done via pressure protection gear.
                    EntitySystem.Get<DamageableSystem>().TryChangeDamage(Owner.Uid, Damage * Atmospherics.LowPressureDamage, true);

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

                    var damageScale = (int) MathF.Min((pressure / Atmospherics.HazardHighPressure) * Atmospherics.PressureDamageCoefficient, Atmospherics.MaxHighPressureDamage);

                    // Deal damage and ignore resistances. Resistance to pressure damage should be done via pressure protection gear.
                    EntitySystem.Get<DamageableSystem>().TryChangeDamage(Owner.Uid, Damage * damageScale, true);

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
