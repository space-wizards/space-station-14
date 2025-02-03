using Content.Shared.Storage;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Tools.Components;


[RegisterComponent, Access(typeof(SliceableSystem))]
public sealed partial class SliceableComponent : Component
{
    /// <summary>
    /// The quality required by the tool being used to slice the sliceable entity
    /// </summary>
    [DataField]
    public ProtoId<ToolQualityPrototype> SlicingQuality = "Slicing";

    /// <summary>
    /// List of entities to spawn after slicing do-after is completed
    /// If no spawned entities are set, any attempts to slice will fail.
    /// </summary>
    [DataField]
    public List<EntitySpawnEntry> SpawnedEntities = new();

    /// <summary>
    /// Sound that places when slicing do-after is completed
    /// </summary>
    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Items/Culinary/chop.ogg");

    /// <summary>
    /// The amount of seconds it takes to complete the slicing do-after
    /// </summary>
    [DataField]
    public TimeSpan DoafterTime = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The min distance the sliced pieces should move from the original space
    /// </summary>
    [DataField]
    public float MinSpawnOffset = 2f;

    /// <summary>
    /// The max distance the sliced pieces should move from the original space
    /// </summary>
    [DataField]
    public float MaxSpawnOffset = 2.5f;
}
