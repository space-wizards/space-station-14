using Content.Shared._FTL.Areas;
using Robust.Shared.Serialization;

namespace Content.Shared._FTL.PowerControl;

/// <summary>
///     Sent to the server to set whether the generator should be on or off
/// </summary>
[Serializable, NetSerializable]
public sealed class ToggleApcMessage : BoundUserInterfaceMessage
{
    public EntityUid ApcEntity;

    public ToggleApcMessage(EntityUid entity)
    {
        ApcEntity = entity;
    }
}

[Serializable, NetSerializable]
public sealed class PowerControlState : BoundUserInterfaceState
{
    public List<Area> Areas;

    public PowerControlState(List<Area> areas)
    {
        Areas = areas;
    }
}

[NetSerializable, Serializable]
public enum PowerControlUiKey : byte
{
    Key,
}

/// <summary>
/// This handles...
/// </summary>
public abstract class SharedPowerControlSystem : EntitySystem
{

}
