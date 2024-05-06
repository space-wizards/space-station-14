using Content.Shared.Storage;

namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// Adds entities randomly to the corridors.
/// </summary>
public sealed partial class CorridorClutterPostGen : IPostDunGen
{
    [DataField]
    public float Chance = 0.05f;

    /// <summary>
    /// The default starting bulbs
    /// </summary>
    [DataField(required: true)]
    public List<EntitySpawnEntry> Contents = new();
}
