using Content.Shared.DoAfter;
using Content.Shared.TapeRecorder.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.TapeRecorder.Events;

[Serializable, NetSerializable]
public sealed partial class TapeCassetteRepairDoAfterEvent : SimpleDoAfterEvent;

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
public sealed class PrintTapeRecorderMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class TapeRecorderState : BoundUserInterfaceState
{
    // TODO: check the itemslot on client instead of putting easy casette stuff in the state
    public bool HasCasette;
    public bool HasData;
    public float CurrentTime;
    public float MaxTime;
    public string CassetteName;
    public TimeSpan PrintCooldown;

    public TapeRecorderState(
        bool hasCasette,
        bool hasData,
        float currentTime,
        float maxTime,
        string cassetteName,
        TimeSpan printCooldown)
    {
        HasCasette = hasCasette;
        HasData = hasData;
        CurrentTime = currentTime;
        MaxTime = maxTime;
        CassetteName = cassetteName;
        PrintCooldown = printCooldown;
    }
}
