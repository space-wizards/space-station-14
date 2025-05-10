using Content.Server.Explosion.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Server.Explosion.Components;

/// <summary>
///     Spawns a protoype when triggered.
/// </summary>
[RegisterComponent, Access(typeof(TriggerSystem))]
public sealed partial class SpawnOnTriggerComponent : Component
{
    /// <summary>
    ///     The prototype to spawn.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Proto = string.Empty;

    /// <summary>
    ///     Use MapCoordinates for spawning?
    ///     Set to true if you don't want the new entity parented to the spawner.
    /// </summary>
    [DataField]
    public bool mapCoords;

    [DataField]
    public int Amount = 1;

    /// <summary>
    ///     #IMP Amount reduces by one for every entity spawned.
    ///     If SingleUse is set to false, this will be reset after all entities spawned.
    /// </summary>
    [DataField]
    public bool SingleUse = true;
}
