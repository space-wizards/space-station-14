using Robust.Shared.GameStates;

namespace Content.Shared.Mind.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MindStatusComponent : Component
{
    /// <summary>
    /// The current status of this entity's mind/player connection
    /// </summary>
    [DataField, AutoNetworkedField]
    public MindStatus Status = MindStatus.Catatonic;
}

public enum MindStatus : byte
{
    /// <summary>
    /// Entity was never controlled by a player (no mind)
    /// </summary>
    Catatonic,

    /// <summary>
    /// Player is connected and alive
    /// </summary>
    Active,

    /// <summary>
    /// Player disconnected while alive (SSD)
    /// </summary>
    SSD,

    /// <summary>
    /// Player is dead but still connected
    /// </summary>
    Dead,

    /// <summary>
    /// Player died and disconnected
    /// </summary>
    DeadAndSSD,

    /// <summary>
    /// Entity is permanently dead with no player ever attached
    /// </summary>
    DeadAndIrrecoverable
}

public sealed class ForceUpdateMindStatusEvent : EntityEventArgs
{
    public EntityUid Entity { get; }

    public ForceUpdateMindStatusEvent(EntityUid entity)
    {
        Entity = entity;
    }
}
