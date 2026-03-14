using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers when an entity is unequipped from another entity.
/// The user is the entity being unequipped from (i.e. the (un)equipee).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnGotUnequippedComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// The slots that being unequipped from will trigger the entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SlotFlags SlotFlags;
}
