namespace Content.Server.Speech.Components
{
    // This controls what name an entity will show in chat if it's not their actual name
    [RegisterComponent]
    public sealed class VoiceChangerVoiceComponent : Component
    {
        public string voiceName = "Bane";
    }
}
