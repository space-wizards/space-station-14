using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Content.Server.Interfaces;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Destructible
{
    /// <summary>
    /// Deletes the entity once a certain damage threshold has been reached.
    /// </summary>
    public class DestructibleComponent : Component, IOnDamageBehavior, IDestroyAct, IExAct
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

        ActSystem _actSystem;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref damageValue, "thresholdvalue", 100);
            serializer.DataField(ref damageType, "thresholdtype", DamageType.Total);
            serializer.DataField(ref spawnOnDestroy, "spawnondestroy", "");
        }

        public override void Initialize()
        {
            base.Initialize();
            _actSystem = _entitySystemManager.GetEntitySystem<ActSystem>();
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
                _actSystem.HandleDestruction(Owner, true);
            }

        }

        void IExAct.OnExplosion(ExplosionEventArgs eventArgs)
        {
            var prob = new Random();
            switch (eventArgs.Severity)
            {
                case ExplosionSeverity.Destruction:
                    _actSystem.HandleDestruction(Owner, false);
                    break;
                case ExplosionSeverity.Heavy:
                    _actSystem.HandleDestruction(Owner, true);
                    break;
                case ExplosionSeverity.Light:
                    if (RandomExtensions.Prob(prob, 40))
                        _actSystem.HandleDestruction(Owner, true);
                    break;
            }

        }


        void IDestroyAct.OnDestroy(DestructionEventArgs eventArgs)
        {
            if (!string.IsNullOrWhiteSpace(spawnOnDestroy) && eventArgs.IsSpawnWreck)
            {
                Owner.EntityManager.TrySpawnEntityAt(spawnOnDestroy, Owner.Transform.GridPosition, out var wreck);
            }
        }
    }
}
