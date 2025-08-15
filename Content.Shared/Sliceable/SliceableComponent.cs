using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Content.Shared.Storage;

namespace Content.Shared.Sliceable;

/// <summary>
/// Allows slice entity via different tools. Slicing by default.
/// </summary>
[RegisterComponent]
public sealed partial class SliceableComponent : Component
{
    /// <summary>
    /// Prototype ID of the entity that will be spawned after slicing.
    /// </summary>
    [DataField]
    public List<EntitySpawnEntry> Slices = [];

    /// <summary>
    /// If true, entity will transfer splitted solution into <see cref"Slices"/>.
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
    /// Time of slicing.
    /// </summary>
    [DataField]
    public TimeSpan SliceTime = TimeSpan.FromSeconds(1);
}
