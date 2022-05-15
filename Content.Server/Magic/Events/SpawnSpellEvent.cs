using Content.Shared.Actions;
using Content.Shared.Storage;

namespace Content.Server.Magic.Events;

public class SpawnSpellEvent : WorldTargetActionEvent
{

    /// <summary>
    /// The list of prototypes this spell will spawn
    /// </summary>
    [DataField("prototypes")]
    public List<EntitySpawnEntry> Contents = new();

    /// <summary>
    /// The offset the prototypes will spawn in on after the first
    /// Set to 0,0 to have them spawn on the same tile.
    /// </summary>
    [DataField("offsetVector2")]
    public Vector2 OffsetVector2;

    /// <summary>
    /// Check to see if these entities should self delete.
    /// </summary>
    [DataField("temporarySummon")]
    public bool TemporarySummon = false;

    /// <summary>
    /// Lifetime to set for the entities to self delete
    /// </summary>
    [DataField("lifetime")]
    public float Lifetime = 10f;
}

