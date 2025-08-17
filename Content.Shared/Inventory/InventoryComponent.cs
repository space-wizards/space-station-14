using Content.Shared.DisplacementMap;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Inventory;

[RegisterComponent, NetworkedComponent]
[Access(typeof(InventorySystem))]
[AutoGenerateComponentState(true)]
public sealed partial class InventoryComponent : Component
{
    /// <summary>
    /// The template defining how the inventory layout will look like.
    /// </summary>
    [DataField, AutoNetworkedField]
    [ViewVariables] // use the API method
    public ProtoId<InventoryTemplatePrototype> TemplateId = "human";

    /// <summary>
    /// For setting the TemplateId.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<InventoryTemplatePrototype> TemplateIdVV
    {
        get => TemplateId;
        set => IoCManager.Resolve<IEntityManager>().System<InventorySystem>().SetTemplateId((Owner, this), value);
    }

    [DataField, AutoNetworkedField]
    public string? SpeciesId;


    [ViewVariables]
    public SlotDefinition[] Slots = Array.Empty<SlotDefinition>();

    [ViewVariables]
    public ContainerSlot[] Containers = Array.Empty<ContainerSlot>();

    [DataField, AutoNetworkedField]
    public Dictionary<string, DisplacementData> Displacements = new();

    /// <summary>
    /// Alternate displacement maps, which if available, will be selected for the player of the appropriate gender.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, DisplacementData> FemaleDisplacements = new();

    /// <summary>
    /// Alternate displacement maps, which if available, will be selected for the player of the appropriate gender.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, DisplacementData> MaleDisplacements = new();
}

/// <summary>
/// Raised if the <see cref="InventoryComponent.TemplateId"/> of an inventory changed.
/// </summary>
[ByRefEvent]
public struct InventoryTemplateUpdated;
