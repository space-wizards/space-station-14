namespace Content.Client.PDA;

[RegisterComponent]
[Access(typeof(PDAVisualizerSystem))]
public sealed class PDAVisualizerComponent : Component
{
    /// <summary>
    /// The base PDA sprite state, eg. "pda", "pda-clown"
    /// </summary>
    [DataField("state")]
    public string? State;
}
