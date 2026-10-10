namespace Content.Shared.Trigger;

/// <summary>
/// Raised when a spoken phrase matched a registered voice command.
/// </summary>
/// <param name="Source">The entity that spoke.</param>
/// <param name="Tag">The opaque tag the matched phrase maps to.</param>
/// <param name="Quantity">Always >= 1.</param>
[ByRefEvent]
public readonly record struct VoiceCommandMatchedEvent(EntityUid Source, string Tag, int Quantity);

/// <summary>
/// Raised during a rebuild to collect phrase -> tag entries from subscribers. Static triggers take priority.
/// Pull-based, so a subscriber whose triggers change must call <c>RebuildVoiceCommandLookup</c> itself.
/// </summary>
[ByRefEvent]
public struct VoiceCommandsGetTriggersEvent
{
    public readonly Dictionary<string, string> Triggers;

    public VoiceCommandsGetTriggersEvent()
    {
        Triggers = new();
    }
}
