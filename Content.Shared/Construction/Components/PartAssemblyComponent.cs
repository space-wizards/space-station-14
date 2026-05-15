using Robust.Shared.Containers;

namespace Content.Shared.Construction.Components;

/// <summary>
/// This is used for construction which requires a set of
/// entities with specific tags to be inserted into another entity.
/// todo: in a pr that isn't 6k loc, combine this with MechAssemblyComponent
/// </summary>
[RegisterComponent]
public sealed partial class PartAssemblyComponent : Component
{
    /// <summary>
    /// A dictionary of a set of parts to a list of tags for each assembly.
    /// </summary>
    [DataField("parts", required: true)]
    public Dictionary<string, List<string>> Parts = new();

    /// <summary>
    /// The entry in <see cref="Parts"/> that is currently being worked on.
    /// </summary>
    [DataField("currentAssembly")]
    public string? CurrentAssembly;

    /// <summary>
    /// The container where the parts are stored
    /// </summary>
    [DataField("containerId")]
    public string ContainerId = "part-container";

    /// <summary>
    /// The container that stores all of the parts when
    /// they're being assembled.
    /// </summary>
    [ViewVariables]
    public Container PartsContainer = default!;
}

/// <summary>
/// Event raised when a valid part is inserted into the part assembly.
/// </summary>
public sealed class PartAssemblyPartInsertedEvent
{

}
