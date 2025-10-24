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
    /// If true, entity will transfer split solution into <see cref="Slices"/>
    /// (if they have components to support process and have required empty volume).
    /// </summary>
    [DataField]
    public bool TransferSolution = true;

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
}
