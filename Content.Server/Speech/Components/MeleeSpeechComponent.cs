namespace Content.Server.Speech.Components
{
    [RegisterComponent]
    public sealed class MeleeSpeechComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("phrase")]
        public string Phrase = "debug";
    }
}
