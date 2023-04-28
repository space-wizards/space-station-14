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
    public string Name { get; }
    public string Voice { get; } // Corvax-TTS

    public VoiceMaskBuiState(string name, string voice)  // Corvax-TTS
    {
        Name = name;
        Voice = voice;  // Corvax-TTS
    }
}

[Serializable, NetSerializable]
public sealed class VoiceMaskChangeNameMessage : BoundUserInterfaceMessage
{
    public string Name { get; }

    public VoiceMaskChangeNameMessage(string name)
    {
        Name = name;
    }
}
