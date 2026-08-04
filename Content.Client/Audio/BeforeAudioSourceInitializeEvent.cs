using Robust.Shared.GameObjects;

namespace Content.Client.Audio;

/// <summary>
/// Raised before the engine initializes a newly created audio source.
/// </summary>
[ByRefEvent]
public record struct BeforeAudioSourceInitializeEvent;
