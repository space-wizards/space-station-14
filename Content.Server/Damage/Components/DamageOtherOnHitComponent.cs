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

<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
<<<<<<< refs/remotes/origin/master
        [DataField("damageType")]
        public DamageType DamageType { get; } = DamageType.Blunt;
=======
        [DataField("damageType",required: true)]
        private readonly string _damageType = default!;
>>>>>>> update damagecomponent across shared and server

=======
>>>>>>> Refactor damageablecomponent update (#4406)
=======
>>>>>>> refactor-damageablecomponent
        [DataField("amount")]
        public int Amount { get; } = 1;

        [DataField("ignoreResistances")]
<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
<<<<<<< refs/remotes/origin/master
        public bool IgnoreResistances { get; } = false;
=======
        private bool _ignoreResistances;
=======
        public bool IgnoreResistances { get; } = false;
>>>>>>> Bring refactor-damageablecomponent branch up-to-date with master (#4510)
=======
        public bool IgnoreResistances { get; } = false;
>>>>>>> refactor-damageablecomponent

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
>>>>>>> update damagecomponent across shared and server
    }
}
