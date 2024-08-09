using Content.Shared.DoAfter;
using Content.Shared.TapeRecorder.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.TapeRecorder.Events;

[Serializable, NetSerializable]
public sealed partial class TapeCassetteRepairDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed class ToggleTapeRecorderMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class ChangeModeTapeRecorderMessage : BoundUserInterfaceMessage
{
    public TapeRecorderMode Mode;

    public ChangeModeTapeRecorderMessage(TapeRecorderMode mode)
    {
        Mode = mode;
    }
}

[Serializable, NetSerializable]
public sealed class PrintTapeRecorderMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class TapeRecorderState : BoundUserInterfaceState
{
    public bool Active;
    public TapeRecorderMode Mode;
    public bool HasCasette;
    public bool HasData;
    public float CurrentTime;
    public float MaxTime;
    public float RewindSpeed;
    public string CassetteName;
    public TimeSpan PrintCooldown;

    public TapeRecorderState(
        bool active,
        TapeRecorderMode mode,
        bool hasCasette,
        bool hasData,
        float currentTime,
        float maxTime,
        float rewindSpeed,
        string cassetteName,
        TimeSpan printCooldown
        )
    {
        Active = active;
        Mode = mode;
        HasCasette = hasCasette;
        HasData = hasData;
        CurrentTime = currentTime;
        MaxTime = maxTime;
        RewindSpeed = rewindSpeed;
        CassetteName = cassetteName;
        PrintCooldown = printCooldown;
    }
}
