using Robust.Shared.Serialization;

namespace Content.Shared.BoomBox;

[Serializable, NetSerializable]
public enum BoomBoxUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class BoomBoxUiState : BoundUserInterfaceState
{
    public bool CanPlusVol { get; }
    public bool CanMinusVol { get; }
    public bool CanStop { get; }
    public bool CanStart { get; }

    public BoomBoxUiState(
        bool canPlusVol,
        bool canMinusVol,
        bool canStop,
        bool canStart)
    {
        CanPlusVol = canPlusVol;
        CanMinusVol = canMinusVol;
        CanStop = canStop;
        CanStart = canStart;
    }
}

[Serializable, NetSerializable]
public sealed class BoomBoxPlusVolMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class BoomBoxMinusVolMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class BoomBoxStartMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class BoomBoxStopMessage : BoundUserInterfaceMessage
{
}
