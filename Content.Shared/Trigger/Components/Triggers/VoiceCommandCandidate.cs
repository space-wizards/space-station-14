namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// A voice trigger's tag and its precomputed trigrams.
/// </summary>
public sealed class VoiceCommandCandidate
{
    public string Tag = string.Empty;

    public HashSet<string> Trigrams = new();
}
