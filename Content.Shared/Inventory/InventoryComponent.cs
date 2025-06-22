using Content.Shared.DisplacementMap;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Inventory;

[RegisterComponent, NetworkedComponent]
[Access(typeof(InventorySystem))]
public sealed partial class InventoryComponent : Component
{
    [DataField]
    public ProtoId<InventoryTemplatePrototype> TemplateId { get; set; } = "human";

    [DataField]
    public string? SpeciesId { get; set; }

    [ViewVariables(VVAccess.ReadWrite)]
    public SlotDefinition[] Slots = Array.Empty<SlotDefinition>();

    [ViewVariables(VVAccess.ReadWrite)]
    public ContainerSlot[] Containers = Array.Empty<ContainerSlot>();

    [DataField]
    public Dictionary<string, DisplacementData> Displacements = new();

    /// <summary>
    /// Alternate displacement maps, which if available, will be selected for the player of the appropriate gender.
    /// </summary>
    [DataField]
    public Dictionary<string, DisplacementData> FemaleDisplacements = new();

    /// <summary>
    /// Alternate displacement maps, which if available, will be selected for the player of the appropriate gender.
    /// </summary>
    [DataField]
    public Dictionary<string, DisplacementData> MaleDisplacements = new();
}

[Serializable, NetSerializable]
public sealed class InventoryComponentState : ComponentState
{
    public ProtoId<InventoryTemplatePrototype> Template;
    public string? Species;

    public InventoryComponentState(ProtoId<InventoryTemplatePrototype> template, string? species)
    {
        Template = template;
        Species = species;
    }
}

/// <summary>
/// Raised if the <see cref="InventoryComponent.TemplateId"/> of an inventory changed.
/// </summary>
[ByRefEvent]
public struct InventoryTemplateUpdated;
