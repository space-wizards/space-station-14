using Content.Shared._MACRO.Body.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared._MACRO.Body.Components;
/// <summary>
/// The entity with this component is attached to an organ via <see cref="EquipmentOrganSystem"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class OrganAttachedComponent : Component
{
    /// <summary>
    /// The organ this component's owner is attached to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? AttachedOrgan;

    /// <summary>
    /// What slot this entity is placed into.
    /// </summary>
    [DataField]
    public string Slot;

    /// <summary>
    /// Determines if this entity is placed in a hand or inventory slot.
    /// </summary>
    [DataField]
    public bool HandEquipment;
}
