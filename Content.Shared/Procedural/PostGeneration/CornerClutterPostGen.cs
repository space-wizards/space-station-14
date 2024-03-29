using Content.Shared.Storage;

namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// Spawns entities inside corners.
/// </summary>
public sealed partial class CornerClutterPostGen : IPostDunGen
{
    [DataField]
    public float Chance = 0.50f;

    /// <summary>
    /// The default starting bulbs
    /// </summary>
    [DataField(required: true)]
    public List<EntitySpawnEntry> Contents = new();
}
