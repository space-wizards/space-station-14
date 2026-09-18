using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Shared.Containers;

/// <summary>
/// Version of <see cref="ContainerFillComponent"/> that utilizes <see cref="EntityTableSelector"/>
/// </summary>
[RegisterComponent, Access(typeof(ContainerFillSystem))]
public sealed partial class EntityTableContainerFillComponent : Component
{
    [DataField]
    public Dictionary<string, EntityTableSelector> Containers = new();

    /// <summary>
    /// Whether to sort the contents of the table by size before inserting.
    /// Helps with fitting items into containers.
    /// </summary>
    [DataField]
    public bool Sort;

    /// <summary>
    /// When true the container will be passed into the table with an <see cref="EntityTableContext"/>.
    /// </summary>
    [DataField]
    public bool ContextContainers;
}
