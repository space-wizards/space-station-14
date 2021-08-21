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
using Robust.Shared.IoC;
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

        // TODO PROTOTYPE Replace this datafield variable with prototype references, once they are supported.
        // Also remove Initialize override, if no longer needed.
        [DataField("damageType")] private readonly string _damageTypeID = "Blunt";
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageTypePrototype DamageType = default!;
        protected override void Initialize()
        {
            base.Initialize();
            DamageType = IoCManager.Resolve<IPrototypeManager>().Index<DamageTypePrototype>(_damageTypeID);
        }

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
                    if (pressure > Atmospherics.WarningLowPressure)
                        goto default;

                    // Deal damage and ignore resistances. Resistance to pressure damage should be done via pressure protection gear.
                    damageable.TryChangeDamage(DamageType, Atmospherics.LowPressureDamage,true);

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

                    // Deal damage and ignore resistances. Resistance to pressure damage should be done via pressure protection gear.
                    damageable.TryChangeDamage(DamageType, damage,true);

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
