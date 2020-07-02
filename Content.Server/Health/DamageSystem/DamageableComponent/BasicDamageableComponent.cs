using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.DamageSystem;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.DamageSystem
{

    /// <summary>
    ///     Component that allows attached IEntities to take damage and be destroyed after a certain threshold.
    /// </summary>
    class BasicDamageableComponent : IDamageableComponent
    {
        public override string Name => "BasicDamageable";

        [ViewVariables]
        public ResistanceSet Resistances { get; private set; }

        [ViewVariables]
        public DamageContainer DamageContainer { get; private set; }

        Dictionary<DamageType, List<DamageThreshold>> Thresholds = new Dictionary<DamageType, List<DamageThreshold>>();

        public event EventHandler<DamageThresholdPassedEventArgs> DamageThresholdPassed;
        public event EventHandler<DamageEventArgs> Damaged;

        public override void Initialize()
        {
            base.Initialize();
            foreach (var damagebehavior in Owner.GetAllComponents<IOnDamageBehavior>())
            {
                //AddThresholdsFrom(damagebehavior);
                Damaged += damagebehavior.OnDamaged;
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            // TODO: Writing.
        }

        public override bool TakeDamage(DamageType damageType, int amount, IEntity source)
        {
            return true;
        }

        public override bool SetDamage(DamageType damageType, int newValue, IEntity source)
        {
            return true;
        }

        public override void HealAllDamage()
        {
            
        }

        public override bool IsDead()
        {
            return false;
        }


    }
}
