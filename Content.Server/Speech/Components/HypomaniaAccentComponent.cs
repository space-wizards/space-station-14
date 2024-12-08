namespace Content.Server.Speech.Components
{
    /// <summary>
    /// This component emphasizes hypomania, specifically by causing laughter after each phrase with a certain probability.
    /// <summary>
    [RegisterComponent]
    public sealed partial class HypomaniaAccentComponent : Component
    {
        // This variable determines the probability of laughter after each phrase.
        [DataField]
        public float LaughChance = 0.5f;
    }
}
