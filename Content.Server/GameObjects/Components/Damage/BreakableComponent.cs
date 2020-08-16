using System.Collections.Generic;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Damage
{
    [RegisterComponent]
    public class BreakableComponent : Component, IOnDamageBehavior, IExAct
    {

        #pragma warning disable 649
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
        #pragma warning restore 649
        /// <inheritdoc />
        public override string Name => "Breakable";
        public DamageThreshold Threshold { get; private set; }

        public DamageType damageType = DamageType.Total;
        public int damageValue = 0;
        public bool broken = false;

        private ActSystem _actSystem;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref damageValue, "thresholdvalue", 100);
            serializer.DataField(ref damageType, "thresholdtype", DamageType.Total);
        }

        public override void Initialize()
        {
            base.Initialize();
            _actSystem = _entitySystemManager.GetEntitySystem<ActSystem>();
        }

        public List<DamageThreshold> GetAllDamageThresholds()
        {
            Threshold = new DamageThreshold(damageType, damageValue, ThresholdType.Breakage);
            return new List<DamageThreshold>() {Threshold};
        }

        public void OnDamageThresholdPassed(object obj, DamageThresholdPassedEventArgs e)
        {
            if (e.Passed && e.DamageThreshold == Threshold && broken == false)
            {
                broken = true;
                _actSystem.HandleBreakage(Owner);
            }
        }

        public void OnExplosion(ExplosionEventArgs eventArgs)
        {
            var prob = IoCManager.Resolve<IRobustRandom>();
            switch (eventArgs.Severity)
            {
                case ExplosionSeverity.Destruction:
                    _actSystem.HandleBreakage(Owner);
                    break;
                case ExplosionSeverity.Heavy:
                    _actSystem.HandleBreakage(Owner);
                    break;
                case ExplosionSeverity.Light:
                    if(prob.Prob(0.4f))
                        _actSystem.HandleBreakage(Owner);
                    break;
            }
        }

    }
}
