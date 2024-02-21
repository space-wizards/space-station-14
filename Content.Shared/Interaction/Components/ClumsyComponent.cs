using Content.Shared.Damage;
using Robust.Shared.Audio;

namespace Content.Shared.Interaction.Components
{
    /// <summary>
    /// A simple clumsy tag-component.
    /// </summary>
    [RegisterComponent]
    public sealed partial class ClumsyComponent : Component
    {
        [DataField(required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier ClumsyDamage = default!;

        /// <summary>
        /// Chance between 0 and 1 that the character will bonk when
        /// trying to climb something.
        /// </summary>
        [DataField]
        public float BonkChance = 0.5f;

        /// <summary>
        ///     Sound to play when clumsy interactions fail
        /// </summary>
        [DataField]
        public SoundSpecifier ClumsySound = new SoundPathSpecifier("/Audio/Items/bikehorn.ogg");
    }
}
