using Content.Shared.Tools;
using Robust.Shared.Audio;
using Content.Shared.Storage;
using Robust.Shared.Prototypes;

namespace Content.Shared.Sliceable;

/// <summary>
/// Allows slice entity via different tools, 'Slicing' by default.
/// Slicing effectively destroys entity at the end of the process,
/// and can provide newly created entities as a result.
/// </summary>
[RegisterComponent, Access(typeof(SliceableSystem))]
public sealed partial class SliceableComponent : Component
{
    /// <summary>
    /// Prototype ID of the entity that will be spawned after slicing.
    /// </summary>
    [DataField]
    public List<EntitySpawnEntry> Slices = [];

    /// <summary>
    /// ToolQuality for slicing.
    /// </summary>
    [DataField]
    public ProtoId<ToolQualityPrototype> ToolQuality = "Slicing";

    /// <summary>
    /// Sound that will be played after slicing.
    /// </summary>
    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Items/Culinary/chop.ogg");

    /// <summary>
    /// How long it takes for entity to be sliced.
    /// </summary>
    [DataField]
    public TimeSpan SliceTime = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Name of the solution that stores reagents to be split. Should be null in case solution should not be split into slices.
    /// </summary>
    [DataField]
    public string? SolutionToSplit = "food";
}
