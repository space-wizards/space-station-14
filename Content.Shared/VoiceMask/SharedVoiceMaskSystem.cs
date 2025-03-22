using Content.Shared.Speech;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.VoiceMask;

[Serializable, NetSerializable]
public enum VoiceMaskUIKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class VoiceMaskBuiState : BoundUserInterfaceState
{
    public readonly string Name;
    public readonly ProtoId<SpeechVerbPrototype>? Verb;
    public readonly ProtoId<SpeechSoundsPrototype>? Sound;

    public VoiceMaskBuiState(string name, ProtoId<SpeechVerbPrototype>? verb, ProtoId<SpeechSoundsPrototype>? sound)
    {
        Name = name;
        Verb = verb;
        Sound = sound;
    }
}

[Serializable, NetSerializable]
public sealed class VoiceMaskChangeNameMessage : BoundUserInterfaceMessage
{
    public readonly string Name;

    public VoiceMaskChangeNameMessage(string name)
    {
        Name = name;
    }
}

/// <summary>
/// Change the speech verb prototype to override, or null to use the user's verb.
/// </summary>
[Serializable, NetSerializable]
public sealed class VoiceMaskChangeVerbMessage : BoundUserInterfaceMessage
{
    public readonly ProtoId<SpeechVerbPrototype>? Verb;

    public VoiceMaskChangeVerbMessage(ProtoId<SpeechVerbPrototype>? verb)
    {
        Verb = verb;
    }
}

/// <summary>
///     Change the speech noise prototype to override, or null to use the user's default noise.
/// </summary>
[Serializable, NetSerializable]
public sealed class VoiceMaskChangeSoundMessage : BoundUserInterfaceMessage
{
    public readonly ProtoId<SpeechSoundsPrototype>? Sound;

    public VoiceMaskChangeSoundMessage(ProtoId<SpeechSoundsPrototype>? sound)
    {
        Sound = sound;
    }
}
