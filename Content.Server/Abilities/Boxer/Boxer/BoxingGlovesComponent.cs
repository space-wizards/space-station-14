using Content.Shared.Sound;
using Content.Shared.Damage;

namespace Content.Server.Abilities.Boxer
{
    /// <summary>
    /// Activates the boxer component if worn.
    /// </summary>
    [RegisterComponent]
    public sealed class BoxingGlovesComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("hitSound")]
        public SoundSpecifier HitSound { get; set; } = new SoundCollectionSpecifier("BoxingHit");

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
