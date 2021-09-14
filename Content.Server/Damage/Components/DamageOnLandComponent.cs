using Content.Shared.Damage;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Damage.Components
{
    [RegisterComponent]
    public sealed class DamageOnLandComponent : Component
    {
        public override string Name => "DamageOnLand";

        [DataField("amount")]
        [ViewVariables(VVAccess.ReadWrite)]
        public int Amount = 1;

        [DataField("ignoreResistances")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool IgnoreResistances;

        // TODO PROTOTYPE Replace this datafield variable with prototype references, once they are supported.
        // Also remove Initialize override, if no longer needed.
        [DataField("damageType")] public readonly string DamageTypeId = "Blunt";

        [ViewVariables(VVAccess.ReadWrite)]
        public DamageTypePrototype DamageType = default!;
    }
}
