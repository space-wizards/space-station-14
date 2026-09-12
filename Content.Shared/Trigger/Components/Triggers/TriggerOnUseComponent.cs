using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers on use in hand.
/// The user is the player holding the item.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnUseComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// Whether the event should be marked as handled when the trigger is raised.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Handled = true;
}
