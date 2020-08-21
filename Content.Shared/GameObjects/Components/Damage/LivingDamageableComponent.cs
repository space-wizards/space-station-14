using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Damage
{
    [RegisterComponent]
    [ComponentReference(typeof(IDamageableComponent))]
    public class LivingDamageableComponent : DamageableComponent
    {
        public override string Name => "LivingDamageable";

        private int _criticalThreshold;
        private int _deadThreshold;

        public override List<DamageState> SupportedDamageStates => new List<DamageState>
            {DamageState.Alive, DamageState.Critical, DamageState.Dead};

        public override DamageState CurrentDamageState =>
            TotalDamage > _deadThreshold
                ? DamageState.Dead
                : TotalDamage > _criticalThreshold
                    ? DamageState.Critical
                    : DamageState.Alive;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _criticalThreshold, "criticalThreshold", 100);
            serializer.DataField(ref _deadThreshold, "deadThreshold", 200);
        }
    }
}
