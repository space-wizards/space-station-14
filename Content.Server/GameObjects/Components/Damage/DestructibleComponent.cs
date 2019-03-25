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

namespace Content.Server.GameObjects.Components.Destructible
{
    /// <summary>
    /// Deletes the entity once a certain damage threshold has been reached.
    /// </summary>
    public class DestructibleComponent : Component, IOnDamageBehavior
    {
        /// <inheritdoc />
        public override string Name => "Destructible";

        /// <summary>
        /// Damage threshold calculated from the values
        /// given in the prototype declaration.
        /// </summary>
        [ViewVariables]
        public DamageThreshold Threshold { get; private set; }

        public DamageType damageType = DamageType.Total;
        public int damageValue = 0;
        public string spawnOnDestroy = "SteelSheet";
        public bool destroyed = false;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref damageValue, "thresholdvalue", 100);
            serializer.DataField(ref damageType, "thresholdtype", DamageType.Total);
            serializer.DataField(ref spawnOnDestroy, "spawnondestroy", "SteelSheet");
        }

        /// <inheritdoc />
        List<DamageThreshold> IOnDamageBehavior.GetAllDamageThresholds()
        {
            Threshold = new DamageThreshold(damageType, damageValue, ThresholdType.Destruction);
            return new List<DamageThreshold>() { Threshold };
        }

        /// <inheritdoc />
        void IOnDamageBehavior.OnDamageThresholdPassed(object obj, DamageThresholdPassedEventArgs e)
        {
            if (e.Passed && e.DamageThreshold == Threshold && destroyed == false)
            {
                destroyed = true;
                var wreck = Owner.EntityManager.SpawnEntity(spawnOnDestroy);
                wreck.Transform.GridPosition = Owner.Transform.GridPosition;
                Owner.EntityManager.DeleteEntity(Owner);
            }
        }
    }
}
