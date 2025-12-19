using Content.Shared.Interaction;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Trigger on <see cref="UserInteractHandEvent"/>, aka when owner clicks on an entity with an empty hand.
/// The trigger user is the entity that got interacted with.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnUserInteractHandComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// Whether the interaction should be marked as handled after it happens.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Handle = true;
}
