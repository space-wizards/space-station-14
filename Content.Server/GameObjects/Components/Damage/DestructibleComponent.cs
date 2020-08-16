using System.Collections.Generic;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Damage
{
    /// <summary>
    /// Deletes the entity once a certain damage threshold has been reached.
    /// </summary>
    [RegisterComponent]
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

        [ViewVariables(VVAccess.ReadWrite)]
        public int damageValue = 0;

        public string spawnOnDestroy = "";

        public string destroySound = "";

        public bool destroyed = false;

        ActSystem _actSystem;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref damageValue, "thresholdvalue", 100);
            serializer.DataField(ref damageType, "thresholdtype", DamageType.Total);
            serializer.DataField(ref spawnOnDestroy, "spawnondestroy", "");
            serializer.DataField(ref destroySound, "destroysound", "");
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
                var pos = Owner.Transform.GridPosition;
                _actSystem.HandleDestruction(Owner, true);
                if(destroySound != string.Empty)
                {
                    EntitySystem.Get<AudioSystem>().PlayAtCoords(destroySound, pos);
                }


            }

        }

        void IExAct.OnExplosion(ExplosionEventArgs eventArgs)
        {
            var prob = IoCManager.Resolve<IRobustRandom>();
            switch (eventArgs.Severity)
            {
                case ExplosionSeverity.Destruction:
                    _actSystem.HandleDestruction(Owner, false);
                    break;
                case ExplosionSeverity.Heavy:
                    var spawnWreckOnHeavy = prob.Prob(0.5f);
                    _actSystem.HandleDestruction(Owner, spawnWreckOnHeavy);
                    break;
                case ExplosionSeverity.Light:
                    if (prob.Prob(0.4f))
                        _actSystem.HandleDestruction(Owner, true);
                    break;
            }

        }


        void IDestroyAct.OnDestroy(DestructionEventArgs eventArgs)
        {
            if (!string.IsNullOrWhiteSpace(spawnOnDestroy) && eventArgs.IsSpawnWreck)
            {
                Owner.EntityManager.SpawnEntity(spawnOnDestroy, Owner.Transform.GridPosition);
            }
        }
    }
}
