using Content.Shared.Storage.Components;
using Content.Shared.Tag;
using Content.Shared.Tools;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Server.Mech.Components;

/// <summary>
/// A component used to create a mech chassis
/// after the correct parts have been placed inside
/// of it.
/// </summary>
/// <remarks>
/// The actual visualization of the parts being inserted is
/// done via <see cref="ItemMapperComponent"/>
/// </remarks>
[RegisterComponent]
public sealed partial class MechAssemblyComponent : Component
{
    /// <summary>
    /// The parts needed to be placed within the assembly,
    /// stored as a tag and a bool tracking whether or not
    /// they're present.
    /// </summary>
    [DataField("requiredParts", required: true, customTypeSerializer: typeof(PrototypeIdDictionarySerializer<bool, TagPrototype>))]
    public Dictionary<string, bool> RequiredParts = new();

    /// <summary>
    /// The prototype spawned when the assembly is finished
    /// </summary>
    [DataField("finishedPrototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string FinishedPrototype = default!;

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
