using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers when an entity is interacted with by another entity. The trigger user is the entity getting interacted with.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnContactInteractionComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// Whether the interaction should be marked as handled after it happens.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Handle = true;
}
