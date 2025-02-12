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
        [DataField("keyPhrase")]
        public string? KeyPhrase;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public int ListenRange { get; private set; } = 4;

        [DataField]
        public bool IsRecording = false;

        [DataField]
        public int MinLength = 3;

        [DataField]
        public int MaxLength = 50;
    }
}
