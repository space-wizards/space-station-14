using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid;

/// <summary>
///     Raised when entity has changed their voice.
///     This doesn't handle gender changes.
/// </summary>
[ByRefEvent]
public record struct VoiceChangedEvent(ProtoId<EmoteSoundsPrototype>? OldVoice, ProtoId<EmoteSoundsPrototype>? NewVoice);
