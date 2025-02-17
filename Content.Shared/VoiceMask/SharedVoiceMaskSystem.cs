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
    public readonly string? Verb;
    public readonly string Voice; // Corvax-TTS

    public VoiceMaskBuiState(string name, string voice, string? verb)
    {
        Name = name;
        Verb = verb;
        Voice = voice;  // Corvax-TTS
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
    public readonly string? Verb;

    public VoiceMaskChangeVerbMessage(string? verb)
    {
        Verb = verb;
    }
}
