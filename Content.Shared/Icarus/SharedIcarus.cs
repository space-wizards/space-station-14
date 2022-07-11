using Content.Shared.Nuke;
using Robust.Shared.Serialization;

namespace Content.Shared.Icarus;

[Serializable, NetSerializable]
public enum IcarusTerminalUiKey
{
    Key,
}

public enum IcarusTerminalStatus : byte
{
    AWAIT_DISKS,
    FIRE_READY,
    FIRE_PREPARING,
    COOLDOWN
}

[Serializable, NetSerializable]
public sealed class IcarusTerminalUiState : BoundUserInterfaceState
{
    public IcarusTerminalStatus Status { get; }
    public int RemainingTime { get; }
    public int CooldownTime { get; }

    public IcarusTerminalUiState(IcarusTerminalStatus status, int remainingTime, int cooldownTime)
    {
        Status = status;
        RemainingTime = remainingTime;
        CooldownTime = cooldownTime;
    }
}

[Serializable, NetSerializable]
public sealed class IcarusTerminalFireMessage : BoundUserInterfaceMessage
{
}
