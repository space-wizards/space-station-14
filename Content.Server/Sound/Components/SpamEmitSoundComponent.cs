using Content.Shared.Sound.Components;

namespace Content.Server.Sound.Components
{
    /// <summary>
    /// Rolls to play a sound every few seconds.
    /// </summary>
    [RegisterComponent]
    public sealed class SpamEmitSoundComponent : BaseEmitSoundComponent
    {
        [DataField("accumulator")]
        public float Accumulator = 0f;

        [DataField("rollInterval")]
        public float RollInterval = 2f;

        [DataField("playChance")]
        public float PlayChance = 0.5f;

        // Always Pvs.
        [DataField("popUp")]
        public string? PopUp;

        [DataField("enabled")]
        public bool Enabled = true;
    }
}
