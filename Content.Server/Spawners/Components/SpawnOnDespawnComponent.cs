using Content.Server.Spawners.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Server.Spawners.Components;

/// <summary>
/// When a <c>TimedDespawnComponent</c> despawns, another one will be spawned in its place.
/// </summary>
[RegisterComponent, Access(typeof(SpawnOnDespawnSystem))]
public sealed partial class SpawnOnDespawnComponent : Component
{
    /// <summary>
    /// Entity prototype to spawn.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Prototype;
}
