using Content.Shared.Examine;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers when the entity is examined (<see cref="ExaminedEvent"/>).
/// The user is the player examining the entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnExaminedComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// Only trigger if the examined entity is within detail range.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RequireInDetailRange = true;
}
