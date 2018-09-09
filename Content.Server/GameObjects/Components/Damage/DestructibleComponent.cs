using System;
using System.Collections.Generic;
using SS14.Shared.GameObjects;
using SS14.Shared.Log;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;
using Content.Server.Interfaces;
using Content.Shared.GameObjects;
using SS14.Shared.Serialization;
using SS14.Shared.ViewVariables;

namespace Content.Server.GameObjects
{
    /// <summary>
    /// Deletes the entity once a certain damage threshold has been reached.
    /// </summary>
    public class DestructibleComponent : Component, IOnDamageBehavior
    {
        /// <inheritdoc />
        public override string Name => "Destructible";

        /// <inheritdoc />
        public override uint? NetID => ContentNetIDs.DESTRUCTIBLE;

        /// <summary>
        /// Damage threshold calculated from the values
        /// given in the prototype declaration.
        /// </summary>
        [ViewVariables]
        public DamageThreshold Threshold { get; private set; }


        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            // TODO: Writing
            if (serializer.Reading)
            {
                DamageType damageType = DamageType.Total;
                int damageValue = 0;

                serializer.DataReadFunction("thresholdtype", DamageType.Total, type => damageType = type);
                serializer.DataReadFunction("thresholdvalue", 0, val => damageValue = val);

                Threshold = new DamageThreshold(damageType, damageValue);
            }
        }

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            if (Owner.TryGetComponent<DamageableComponent>(out DamageableComponent damageable))
            {
                damageable.DamageThresholdPassed += OnDamageThresholdPassed;
            }
        }

        /// <inheritdoc />
        public List<DamageThreshold> GetAllDamageThresholds()
        {
            return new List<DamageThreshold>() { Threshold };
        }

        /// <inheritdoc />
        public void OnDamageThresholdPassed(object obj, DamageThresholdPassedEventArgs e)
        {
            if (e.Passed && e.DamageThreshold == Threshold)
            {
                Owner.EntityManager.DeleteEntity(Owner);
            }
        }
    }
}
