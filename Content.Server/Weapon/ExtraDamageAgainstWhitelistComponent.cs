using Content.Server.Weapon.Melee;
using Content.Shared.Whitelist;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Weapon
{
    /// <summary>
    ///     Applies extra damage to
    /// </summary>
    [RegisterComponent, Friend(typeof(MeleeWeaponSystem))]
    public class ExtraDamageAgainstWhitelistComponent : Component
    {
        public override string Name { get; } = "ExtraDamageAgainstWhitelist";

        [DataField("whitelist", required: true)]
        public EntityWhitelist Whitelist = default!;

        // TODO Change to use resistanceset/damageset/whatever so this can be of arbitrary type

        [DataField("damageMultiplier")]
        public int DamageMultiplier = 1;

        [DataField("flatDamage")]
        public int FlatDamage = 0;
    }
}
