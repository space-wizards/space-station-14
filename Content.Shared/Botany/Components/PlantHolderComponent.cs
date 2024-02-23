using Content.Shared.Botany.Systems;
using Robust.Shared.Containers;

namespace Content.Shared.Botany.Components;

/// <summary>
/// Holds a single plant entity.
/// Plants can be created by using seed packets on a plant holder.
/// Plants can be deleted by using a spade.
/// </summary>
[RegisterComponent, Access(typeof(PlantHolderSystem))]
public sealed partial class PlantHolderComponent : Component
{
    /// <summary>
    /// The plant container's id.
    /// </summary>
    [DataField]
    public string PlantContainerId = "plant";

    /// <summary>
    /// The plant container.
    /// Stores a single plant entity.
    /// </summary>
    [ViewVariables]
    public ContainerSlot PlantContainer = default!;

    [ViewVariables]
    public EntityUid? PlantEntity => PlantContainer.ContainedEntity;
}
