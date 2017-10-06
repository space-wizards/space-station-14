using System;
using System.Collections.Generic;
using SS14.Shared.GameObjects;
using SS14.Shared.Log;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;
using Content.Server.Interfaces;
using Content.Shared.GameObjects;


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
        public DamageThreshold Threshold { get; private set; }

        /// <inheritdoc />
        public override void LoadParameters(YamlMappingNode mapping)
        {
            //TODO currently only supports one threshold pair; gotta figure out YAML better

            YamlNode node;

            DamageType damageType = DamageType.Total;
            int damageValue = 0;

            if (mapping.TryGetNode("thresholdtype", out node))
            {
                damageType = node.AsEnum<DamageType>();
            }
            if (mapping.TryGetNode("thresholdvalue", out node))
            {
                damageValue = node.AsInt();
            }

            Threshold = new DamageThreshold(damageType, damageValue);
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
