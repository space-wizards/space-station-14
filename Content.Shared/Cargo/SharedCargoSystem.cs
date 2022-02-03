using Robust.Shared.GameObjects;

namespace Content.Shared.Cargo;

public abstract class SharedCargoSystem : EntitySystem {}

public enum CargoTelepadState : byte
{
    Unpowered,
    Idle,
    Teleporting,
};

public enum CargoTelepadVisuals : byte
{
    State,
};
