using Content.Shared.Tools;
using Robust.Shared.Audio;
using Content.Shared.Storage;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

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
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
    public string ToolQuality = "Slicing";

    /// <summary>
    /// Sound that will be played after slicing.
    /// </summary>
    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Items/Culinary/chop.ogg");

    /// <summary>
    /// Time of slicing.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer), required: true))]
    public TimeSpan SliceTime;
}
