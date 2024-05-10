using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.Storage;

namespace Content.Shared.Magic.Events;

// TODO: This class needs combining with InstantSpawnSpellEvent

public sealed partial class WorldSpawnSpellEvent : WorldTargetActionEvent, ISpeakSpell
{
    /// <summary>
    /// The list of prototypes this spell will spawn
    /// </summary>
    [DataField]
    public List<EntitySpawnEntry> Prototypes = new();

    // TODO: This offset is liable for deprecation.
    // TODO: Target tile via code instead?
    /// <summary>
    /// The offset the prototypes will spawn in on relative to the one prior.
    /// Set to 0,0 to have them spawn on the same tile.
    /// </summary>
    [DataField]
    public Vector2 Offset;

    /// <summary>
    /// Lifetime to set for the entities to self delete
    /// </summary>
    [DataField]
    public float? Lifetime;

    [DataField]
    public string? Speech { get; private set; }
}
