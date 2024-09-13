using Content.Server.Nutrition.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Nutrition.Components;

[RegisterComponent, Access(typeof(SliceableFoodSystem))]
public sealed partial class SliceableFoodComponent : Component
{
    /// <summary>
    /// Prototype to spawn after slicing.
    /// If null then it can't be sliced.
    /// </summary>
    [DataField]
    public EntProtoId? Slice;

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Items/Culinary/chop.ogg");

    /// <summary>
    /// Number of slices the food starts with.
    /// </summary>
    [DataField("count")]
    public ushort TotalCount = 5;

    /// <summary>
    /// how long it takes for this food to be sliced
    /// </summary>
    [DataField]
    public float SliceTime = 1f;

    /// <summary>
    /// all the pieces will be shifted in random directions.
    /// </summary>
    [DataField]
    public float SpawnOffset = 0.5f;
}
