using Robust.Shared.Audio;

namespace Content.Server.Sports.Components
{

    /// <summary>
    /// Give this to a melee weapon with wide attack and it will be able to bat thrown objects
    /// </summary>
    [RegisterComponent]
    public sealed class BaseballBatComponent : Component
    {
        [DataField("wackForceMultiplierMin")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float WackForceMultiplierMin {get; set; } = 0.75f;

        [DataField("wackForceMultiplierMax")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float WackForceMultiplierMax {get; set; } = 5f;

        /// <summary>
        /// If set to true then the bat can only hit items being thrown
        /// </summary>
        [DataField("onlyHitThrown")]
        [ViewVariables(VVAccess.ReadOnly)]
        public bool OnlyHitThrown = false;

        /// <summary>
        /// Sound that plays when you have a bad hit.
        /// A good hit is when the item is going to go less far than the target
        /// </summary>
        [DataField("badHitSound")]
        [ViewVariables(VVAccess.ReadOnly)]
        public SoundSpecifier BadHitSound = new SoundPathSpecifier("/Audio/Effects/hit_kick.ogg");

        /// <summary>
        /// Sound that plays when you have a good hit.
        /// A good hit is when the item is going to go at least as far as the target
        /// </summary>
        [DataField("goodHitSound")]
        [ViewVariables(VVAccess.ReadOnly)]
        public SoundSpecifier GoodHitSound = new SoundPathSpecifier("/Audio/Effects/hit_kick.ogg");

        /// <summary>
        /// Chances of the item being hit turning into a fireball.
        /// Should be a very high number. Set to 1 if you always want a fireball
        /// </summary>
        [DataField("fireballChance")]
        [ViewVariables(VVAccess.ReadWrite)]
        public int FireballChance {get; set; } = 10;
    }
}
