using Content.Server.Damage.Systems;
using Content.Shared.Damage;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Prototypes;
using Robust.Shared.IoC;

namespace Content.Server.Damage.Components
{
    [Friend(typeof(DamageOtherOnHitSystem))]
    [RegisterComponent]
    public class DamageOtherOnHitComponent : Component
    {
        public override string Name => "DamageOtherOnHit";

        [DataField("amount")]
        public int Amount { get; } = 1;

        [DataField("ignoreResistances")]
        public bool IgnoreResistances { get; } = false;

        // TODO PROTOTYPE Replace this datafield variable with prototype references, once they are supported.
        // Also remove Initialize override, if no longer needed.
        [DataField("damageType")]
        private readonly string _damageTypeID = "Blunt";
        public DamageTypePrototype DamageType { get; set; } =  default!;
        protected override void Initialize()
        {
            base.Initialize();
            DamageType = IoCManager.Resolve<IPrototypeManager>().Index<DamageTypePrototype>(_damageTypeID);
        }
    }
}
