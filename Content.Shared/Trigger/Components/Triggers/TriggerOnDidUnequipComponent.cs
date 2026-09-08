using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers when an entity unequips another entity.
/// The user is the entity being unequipped.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnDidUnequipComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// The slots that entities being unequipped from will trigger the entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SlotFlags SlotFlags;
}
