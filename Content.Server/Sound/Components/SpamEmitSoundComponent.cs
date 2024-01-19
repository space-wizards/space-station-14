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

        [DataField]
        public float RollInterval = 2f;

        [DataField]
        public float MaxExtraInterval = 2f;

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
