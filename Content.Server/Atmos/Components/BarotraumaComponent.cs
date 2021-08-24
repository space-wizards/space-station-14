using System;
using System.Runtime.CompilerServices;
using Content.Server.Alert;
using Content.Server.Pressure;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Robust.Shared.GameObjects;
<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
=======
using Robust.Shared.Serialization.Manager.Attributes;
<<<<<<< refs/remotes/origin/master
using Dependency = Robust.Shared.IoC.DependencyAttribute;
>>>>>>> update damagecomponent across shared and server
=======
using Robust.Shared.Prototypes;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;
>>>>>>> Refactor damageablecomponent update (#4406)
=======
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Prototypes;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;
>>>>>>> refactor-damageablecomponent

namespace Content.Server.Atmos.Components
{
    /// <summary>
    ///     Barotrauma: injury because of changes in air pressure.
    /// </summary>
    [RegisterComponent]
    public class BarotraumaComponent : Component
    {
        public override string Name => "Barotrauma";

<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
<<<<<<< refs/remotes/origin/master
=======
        [DataField("damageType", required: true)]
        private readonly string _damageType = default!;
=======
=======
>>>>>>> refactor-damageablecomponent
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
<<<<<<< HEAD
>>>>>>> Refactor damageablecomponent update (#4406)

>>>>>>> update damagecomponent across shared and server
=======

>>>>>>> refactor-damageablecomponent
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

<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
                    damageable.ChangeDamage(DamageType.Blunt, Atmospherics.LowPressureDamage, false, Owner);
=======
                    // Deal damage and ignore resistances. Resistance to pressure damage should be done via pressure protection gear.
                    damageable.TryChangeDamage(DamageType, Atmospherics.LowPressureDamage,true);
>>>>>>> Refactor damageablecomponent update (#4406)
=======
                    // Deal damage and ignore resistances. Resistance to pressure damage should be done via pressure protection gear.
                    damageable.TryChangeDamage(DamageType, Atmospherics.LowPressureDamage,true);
>>>>>>> refactor-damageablecomponent

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

<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
<<<<<<< refs/remotes/origin/master
                    damageable.ChangeDamage(DamageType.Blunt, damage, false, Owner);
=======
                    damageable.ChangeDamage(damageable.GetDamageType(_damageType), damage, false, Owner);
>>>>>>> update damagecomponent across shared and server
=======
                    // Deal damage and ignore resistances. Resistance to pressure damage should be done via pressure protection gear.
                    damageable.TryChangeDamage(DamageType, damage,true);
>>>>>>> Refactor damageablecomponent update (#4406)
=======
                    // Deal damage and ignore resistances. Resistance to pressure damage should be done via pressure protection gear.
                    damageable.TryChangeDamage(DamageType, damage,true);
>>>>>>> refactor-damageablecomponent

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
