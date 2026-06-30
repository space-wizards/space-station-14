using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers when an entity is equipped to another entity.
/// The user is the entity being equipped to (i.e. the equipee).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnGotEquippedComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// The slots that being equipped to will trigger the entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SlotFlags SlotFlags;
}
