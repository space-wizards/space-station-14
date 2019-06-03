using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Content.Server.Interfaces;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Destructible
{
    /// <summary>
    /// Deletes the entity once a certain damage threshold has been reached.
    /// </summary>
    public class DestructibleComponent : Component, IOnDamageBehavior, IDestroyAct
    {
        #pragma warning disable 649
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
        #pragma warning restore 649

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
        public string spawnOnDestroy = "";
        public bool destroyed = false;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref damageValue, "thresholdvalue", 100);
            serializer.DataField(ref damageType, "thresholdtype", DamageType.Total);
            serializer.DataField(ref spawnOnDestroy, "spawnondestroy", "");
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
                var actSystem = _entitySystemManager.GetEntitySystem<ActSystem>();
                actSystem.HandleDestruction(Owner, e.ExcessDamage);
                Owner.Delete();
            }

        }
        void IDestroyAct.Destroy(DestructionEventArgs eventArgs)
        {
            if (!string.IsNullOrWhiteSpace(spawnOnDestroy) && Owner.EntityManager.TrySpawnEntityAt(spawnOnDestroy, Owner.Transform.GridPosition, out var wreck))
            {
                if (wreck.TryGetComponent<DamageableComponent>(out var component))
                {
                    if (eventArgs.Damage > 0)
                    {
                        Logger.DebugS("Second-hand Destruction", "Wreckage (UID {0}) received {1} damage.", wreck.Uid, eventArgs.Damage);
                    }
                    component.TakeDamage(eventArgs.TypeOfDamage, eventArgs.Damage);
                }
            }
        }
    }
}
