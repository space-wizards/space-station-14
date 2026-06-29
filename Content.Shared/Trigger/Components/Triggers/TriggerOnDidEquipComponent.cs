using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers when an entity equips another entity.
/// The user is the entity being equipped.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnDidEquipComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// The slots entities being equipped to will trigger the entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SlotFlags SlotFlags;
}
