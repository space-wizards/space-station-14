namespace Content.Server.Speech.Components
{
    [RegisterComponent]
    public sealed partial class StutteringAccentComponent : Component
    {
        [DataField("matchRandomProb")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float MatchRandomProb = 0.8f;

        [DataField("fourRandomProb")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float FourRandomProb = 0.1f;

        [DataField("threeRandomProb")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float ThreeRandomProb = 0.2f;

        [DataField("cutRandomProb")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float CutRandomProb = 0.05f;
    }
}
