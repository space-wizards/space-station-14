using Robust.Shared.Serialization;

namespace Content.Shared.Cargo;

[NetSerializable, Serializable]
public enum CargoConsoleUiKey : byte
{
    Orders,
    Shuttle,
    Telepad
}

public abstract class SharedCargoSystem : EntitySystem {}

[Serializable, NetSerializable]
public enum CargoTelepadState : byte
{
    Unpowered,
    Idle,
    Teleporting,
};

[Serializable, NetSerializable]
public enum CargoTelepadVisuals : byte
{
    State,
};
