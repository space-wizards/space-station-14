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

    /// <summary>
    /// For determining whether worn clothing will be displaced, if the clothing has a species-specific state that is related to this component's SpeciesID.
    /// </summary>
    /// <remarks>
    /// This datafield is here to fix a mothroach bug:
    /// Currently, mothroaches have an InventoryComponent with the SpeciesID and TemplateID set to hamster. This allows them to use the same slots as, and wear the same clothing as, hamsters.
    /// Most of the hats which hamsters can wear have hamster-specific sprite-states. These sprites need to be affected by the mothroaches' displacement map in order to appear in the correct position on the mothroach.
    /// However, because the mothroaches' SpeciesID is hamster, and because the sprite being displayed has a hamster-state, the game will not displace the clothing, under the assumption that a hamster-specific sprite should be placed correctly on our "hamster" (mothroach).
    /// Setting this datafield to `true` will ignore the check that looks for a matching SpeciesID and sprite-state, allowing hamster clothing to be displaced properly on mothroaches.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public bool DisplaceSpeciesAppropriateClothing = false;

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
