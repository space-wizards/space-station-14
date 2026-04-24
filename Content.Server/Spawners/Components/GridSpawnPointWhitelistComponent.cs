using Content.Shared.Whitelist;

namespace Content.Server.Spawners.Components;

/// <summary>
/// Defines whitelist and blacklist for entities that can spawn at a spawnpoint when they are spawned via the <see cref="RuleGridsSystem"/>
/// </summary>
[RegisterComponent]
public sealed partial class GridSpawnPointWhitelistComponent : Component
{
    /// <summary>
    /// Whitelist of entities that can be spawned at this SpawnPoint
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Whitelist of entities that can't be spawned at this SpawnPoint
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;
}
