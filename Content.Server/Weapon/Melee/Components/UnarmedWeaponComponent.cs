using Content.Shared.Sound;
using Content.Shared.Damage;

namespace Content.Server.Weapon.Melee.Components
{
    /// <summary>
    /// Changes the user's unarmed attacks when equipped.
    /// </summary>
    [RegisterComponent]
    public sealed class UnarmedWeaponComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("hitSound")]
        public SoundSpecifier HitSound { get; set; } = new SoundCollectionSpecifier("GenericHit");

        /// <summary>
        /// Is the component currently being worn and affecting someone?
        /// Making the unequip check not totally CBT
        /// </summary>
        public bool IsActive = false;

        [DataField("damage", required:true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;

        /// <summary>
        /// Old damage to restore when we take these off
        /// <summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier OldDamage = default!;
    }
}
