namespace Content.Server.Poly.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class PolyComponent : Component
{
    /// <summary>
    /// When true, Poly will not save it's memory again this round.
    /// </summary>
    [DataField]
    public bool SavedMemory { get; set; } = false;

    /// <summary>
    /// This bird has seen some shit.
    /// Current round memory, gets flushed to the database on round end. Prioritized in the random selection.
    /// </summary>
    [DataField]
    public HashSet<(string Channel, string Sentence, Guid? author)> Memory { get; set; } = new();

    [DataField]
    public HashSet<(string Channel, string Sentence)> SpeechBuffer = new();

    /// <summary>
    /// How likely is Poly to learn any sentence it hears over the radio?
    /// </summary>
    [DataField]
    public float RadioLearnProbability { get; set; } = 0.15f; // TODO: Tweak probabilities

    [DataField]
    public EntityUid Headset { get; set; }

    /// <summary>
    /// How likely is Poly to learn any sentence it hears in person?
    /// </summary>
    [DataField]
    public float LocalLearnProbability { get; set; } = 0.25f;

    public TimeSpan StateTime = TimeSpan.Zero;

    /// <summary>
    /// How long does Poly have to wait between learns. Exists to avoid putting half the round into memory
    /// </summary>
    [DataField]
    public TimeSpan LearnCooldown = TimeSpan.FromSeconds(1); // Should be 60+ seconds on release

    [DataField]
    public TimeSpan BarkAccumulator = TimeSpan.Zero;

    /// <summary>
    /// How often does Poly say something random
    /// </summary>
    [DataField]
    public TimeSpan BarkTime = TimeSpan.FromSeconds(10); // Should probably be a while, I don't think people will like it if this dumb bird floods the chat
}
