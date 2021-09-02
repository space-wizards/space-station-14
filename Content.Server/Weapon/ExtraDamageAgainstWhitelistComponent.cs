using Content.Shared.Whitelist;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Weapon
{
    [RegisterComponent]
    public class ExtraDamageAgainstWhitelistComponent : Component
    {
        public override string Name { get; } = "ExtraDamageAgainstWhitelist";

        [DataField("whitelist", required: true)]
        public EntityWhitelist Whitelist = default!;

        [DataField("damageMultiplier", required: true)]
        public int DamageMultiplier = 1;
    }
}
