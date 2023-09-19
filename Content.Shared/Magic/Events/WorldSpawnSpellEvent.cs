using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.Storage;

namespace Content.Shared.Magic.Events;

public sealed partial class WorldSpawnSpellEvent : WorldTargetActionEvent, ISpeakSpell
{
    // TODO:This class needs combining with InstantSpawnSpellEvent

    /// <summary>
    /// The list of prototypes this spell will spawn
    /// </summary>
    [DataField("prototypes")]
    public List<EntitySpawnEntry> Contents = new();

    // TODO: This offset is liable for deprecation.
    /// <summary>
    /// The offset the prototypes will spawn in on relative to the one prior.
    /// Set to 0,0 to have them spawn on the same tile.
    /// </summary>
    [DataField("offset")]
    public Vector2 Offset;

    /// <summary>
    /// Lifetime to set for the entities to self delete
    /// </summary>
    [DataField("lifetime")] public float? Lifetime;

    [DataField("speech")]
    public string? Speech { get; private set; }
}
