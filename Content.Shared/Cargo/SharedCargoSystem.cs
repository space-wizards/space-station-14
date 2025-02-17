using Robust.Shared.Serialization;

namespace Content.Shared.Cargo;

[NetSerializable, Serializable]
public enum CargoConsoleUiKey : byte
{
    Orders,
    Bounty,
    Shuttle,
    Telepad
}

[NetSerializable, Serializable]
public enum CargoPalletConsoleUiKey : byte
{
    Sale
}

public abstract class SharedCargoSystem : EntitySystem {}

[Serializable, NetSerializable]
public enum CargoTelepadState : byte
{
    Unpowered,
    Idle,
    Teleporting,
};

// [Serializable, NetSerializable] #imp edit to remove default visuals
// public enum CargoTelepadVisuals : byte
// {
//     State,
// };
