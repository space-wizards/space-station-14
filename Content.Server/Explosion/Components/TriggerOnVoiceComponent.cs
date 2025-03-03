namespace Content.Server.Explosion.Components
{
    /// <summary>
    /// Sends a trigger when the keyphrase is heard
    /// </summary>
    [RegisterComponent]
    public sealed partial class TriggerOnVoiceComponent : Component
    {
        public bool IsListening => IsRecording || !string.IsNullOrWhiteSpace(KeyPhrase);

        [ViewVariables(VVAccess.ReadWrite)]
        public string? KeyPhrase;

        [DataField]
        public LocId? DefaultKeyPhrase;

        [DataField]
        public int ListenRange { get; private set; } = 4;

        [DataField]
        public bool IsRecording;

        [DataField]
        public int MinLength = 3;

        [DataField]
        public int MaxLength = 50;
    }
}
