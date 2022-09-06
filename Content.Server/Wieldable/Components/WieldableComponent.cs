using Robust.Shared.Audio;

namespace Content.Server.Wieldable.Components
{
    /// <summary>
    ///     Used for objects that can be wielded in two or more hands,
    /// </summary>
    [RegisterComponent, Access(typeof(WieldableSystem))]
    public sealed class WieldableComponent : Component
    {
        [DataField("wieldSound")]
        public SoundSpecifier? WieldSound = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg");

        [DataField("unwieldSound")]
        public SoundSpecifier? UnwieldSound;

        /// <summary>
        ///     Number of free hands required (excluding the item itself) required
        ///     to wield it
        /// </summary>
        [DataField("freeHandsRequired")]
        public int FreeHandsRequired = 1;

        public bool Wielded = false;

        [DataField("wieldedInhandPrefix")]
        public string WieldedInhandPrefix = "wielded";

        public string? OldInhandPrefix = null;

        [DataField("wieldTime")]
        public float WieldTime = 1.5f;
    }
}
