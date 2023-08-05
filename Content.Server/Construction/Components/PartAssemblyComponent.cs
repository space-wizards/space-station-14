using Content.Server.Mech.Components;
using Content.Shared.Tools;
using Robust.Shared.Containers;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Construction.Components;

/// <summary>
/// This is used for construction which requires a set of
/// entities with specific tags to be inserted into another entity.
/// todo: in a pr that isn't 5k loc, combine this with <see cref="MechAssemblyComponent"/>
/// </summary>
[RegisterComponent]
public sealed class PartAssemblyComponent : Component
{
    /// <summary>
    /// A dictionary of a set of parts to a list of tags for each part.
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

    /// <summary>
    /// The quality of tool needed to remove all the parts
    /// from the parts container.
    /// </summary>
    [DataField("qualityNeeded", customTypeSerializer: typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
    public string QualityNeeded = "Prying";
}
