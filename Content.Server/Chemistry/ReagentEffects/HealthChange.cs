using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Solution;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;

namespace Content.Server.Chemistry.ReagentEffects
{
    /// <summary>
    /// Default metabolism for medicine reagents. Attempts to find a DamageableComponent on the target,
    /// and to update its damage values.
    /// </summary>
    public class HealthChange : ReagentEffect, ISerializationHooks
    {
        /// <summary>
        /// How much damage is changed when 1u of the reagent is metabolized.
        /// </summary>
        [DataField("healthChange")]
        public float AmountToChange { get; set; } = 1.0f;

        // TODO DAMAGE UNITS When damage units support decimals, get rid of this.
        // See also _accumulatedDamage in ThirstComponent and HungerComponent
        private float _accumulatedDamage;

        /// <summary>
        /// Damage group to change.
        /// </summary>
        // TODO PROTOTYPE Replace this datafield variable with prototype references, once they are supported.
        // Also remove ISerializationHooks, if no longer needed.
        [DataField("damageGroup", required: true)]
        private readonly string _damageGroupID = default!;
        public DamageGroupPrototype DamageGroup = default!;
        void ISerializationHooks.AfterDeserialization()
        {
            DamageGroup = IoCManager.Resolve<IPrototypeManager>().Index<DamageGroupPrototype>(_damageGroupID);
        }

        /// <summary>
        ///     Changes damage if a DamageableComponent can be found.
        /// </summary>
        public override void Metabolize(IEntity solutionEntity, Solution.ReagentQuantity amount)
        {
            if (solutionEntity.TryGetComponent(out IDamageableComponent? damageComponent))
            {
<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
<<<<<<< refs/remotes/origin/master
<<<<<<< refs/remotes/origin/master:Content.Server/Chemistry/ReagentEffects/HealthChange.cs
                health.ChangeDamage(DamageType, (int)AmountToChange, true);
=======
                damageComponent.ChangeDamage(damageComponent.GetDamageType(damageType), (int)AmountToChange, true);
>>>>>>> Fix Merge issues
                float decHealthChange = (float) (AmountToChange - (int) AmountToChange);
=======
                damageComponent.ChangeDamage(damageComponent.GetDamageType(damageType), (int)HealthChange, true);
                float decHealthChange = (float) (HealthChange - (int) HealthChange);
>>>>>>> update damagecomponent across shared and server:Content.Server/Chemistry/Metabolism/HealthChangeMetabolism.cs
                _accumulatedHealth += decHealthChange;
=======
                damageComponent.TryChangeDamage(DamageGroup, (int)AmountToChange, true);

                float decHealthChange = (float) (AmountToChange - (int) AmountToChange);
                _accumulatedDamage += decHealthChange;
>>>>>>> Refactor damageablecomponent update (#4406)
=======
                damageComponent.TryChangeDamage(DamageGroup, (int)AmountToChange, true);

                float decHealthChange = (float) (AmountToChange - (int) AmountToChange);
                _accumulatedDamage += decHealthChange;
>>>>>>> refactor-damageablecomponent

                if (_accumulatedDamage >= 1)
                {
                    damageComponent.TryChangeDamage(DamageGroup, 1, true);
                    _accumulatedDamage -= 1;
                }

                else if(_accumulatedDamage <= -1)
                {
                    damageComponent.TryChangeDamage(DamageGroup, -1, true);
                    _accumulatedDamage += 1;
                }
            }
        }
    }
}
