using Robust.Shared.Serialization;

namespace Content.Shared.PDA;

public abstract class SharedRingerSystem : EntitySystem
{
    public const int RingtoneLength = 6;
    public const int NoteTempo = 300;
    public const float NoteDelay = 60f / NoteTempo;
}

[Serializable, NetSerializable]
public enum Note : byte
{
    A,
    Asharp,
    B,
    C,
    Csharp,
    D,
    Dsharp,
    E,
    F,
    Fsharp,
    G,
    Gsharp
}
