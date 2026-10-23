using System.ComponentModel.DataAnnotations;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._MACRO.Body.Components;

/// <summary>
/// Organs with this component equip entities to certain slots that persist when the organ is removed.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EquipmentOrganComponent : Component
{
    /// <summary>
    /// The equipment to place in said slot.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId> Equipment;

    /// <summary>
    /// The container ID used to store equipment.
    /// </summary>
    [DataField]
    public string ContainerId = "item-organ-item-container";
}
