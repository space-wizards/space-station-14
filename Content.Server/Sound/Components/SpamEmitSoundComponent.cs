using Content.Shared.Sound.Components;

namespace Content.Server.Sound.Components
{
    /// <summary>
    /// Rolls to play a sound every few seconds.
    /// </summary>
    [RegisterComponent]
    public sealed partial class SpamEmitSoundComponent : BaseEmitSoundComponent
    {
        [DataField]
        public float Accumulator = 0f;

        /// <summary>
        /// Time in seconds between each roll.
        /// </summary>
        [DataField]
        public float RollInterval = 2f;

        /// <summary>
        /// The maximum amount of extra time (in seconds) that can be
        /// randomly added to the RollInterval after each roll.
        /// </summary>
        [DataField]
        public float MaxExtraInterval = 2f;

        /// <summary>
        /// Chance that the sound will play on each roll.
        /// 0 = never, 1 = always
        /// </summary>
        [DataField]
        public float PlayChance = 0.5f;

        // Always Pvs.
        [DataField]
        public string? PopUp;

        [DataField]
        public bool Enabled = true;

        public float ExtraInterval;
    }
}
