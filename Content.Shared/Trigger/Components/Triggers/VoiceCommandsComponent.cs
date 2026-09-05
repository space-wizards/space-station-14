namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Fuzzy-matches heard phrases against registered triggers and raises <see cref="VoiceCommandMatchedEvent"/>.
/// Requires <see cref="TriggerOnVoiceComponent"/>.
/// </summary>
// Not networked; only read server-side during ListenEvent.
[RegisterComponent]
public sealed partial class VoiceCommandsComponent : Component
{
    /// <summary>Spoken phrase to tag.</summary>
    [DataField]
    public Dictionary<string, string> Triggers = new();

    /// <summary>Trigram overlap threshold in [0, 1]. Lower is more permissive.</summary>
    [DataField]
    public float MatchThreshold = 0.5f;

    /// <summary>Matchable form of every trigger.</summary>
    [ViewVariables]
    public List<VoiceCommandCandidate> Candidates = new();
}
