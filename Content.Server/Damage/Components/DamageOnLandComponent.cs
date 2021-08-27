using Content.Shared.Damage;
using Content.Shared.Throwing;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Damage.Components
{
    [RegisterComponent]
    public class DamageOnLandComponent : Component, ILand
    {
        public override string Name => "DamageOnLand";

        [DataField("ignoreResistances")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool IgnoreResistances = false;

        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;

        void ILand.Land(LandEventArgs eventArgs)
        { 
            Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, new TryChangeDamageEvent(Damage, IgnoreResistances), false);
        }
    }
}
