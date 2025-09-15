using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers when the entity is overlapped for the specified duration.
/// The user is the entity that passes the time threshold while colliding.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnTimedCollideComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// The time an entity has to collide until the trigger is activated.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Threshold = TimeSpan.FromSeconds(1);

    /// <summary>
    /// A collection of entities that are currently colliding with this, and their own unique accumulator.
    /// </summary>
    /// <remarks>
    /// TODO: Add AutoPausedField and (de)serialize values as time offsets when https://github.com/space-wizards/RobustToolbox/issues/3768 is fixed.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public Dictionary<EntityUid, TimeSpan> Colliding = new();
}
