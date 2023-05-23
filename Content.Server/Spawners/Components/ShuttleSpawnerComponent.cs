using Content.Server.Spawners.EntitySystems;

namespace Content.Server.Spawners.Components;

/// <summary>
/// On map init, replaces the spawner with the specified .
/// </summary>
/// <remarks>
/// It is not currently implemented but in the future it may automatically orient and dock to specified airlocks.
/// They only exist right now for mappers.
/// </remarks>
[RegisterComponent, Access(typeof(ShuttleSpawnerSystem))]
public sealed class ShuttleSpawnerComponent : Component
{
    /// <summary>
    /// Path to the grid yml to load on mapinit.
    /// </summary>
    [DataField("path"), ViewVariables(VVAccess.ReadWrite)]
    public string Path = string.Empty;

    /// <summary>
    /// List of airlocks to attempt to automatically dock to.
    /// </summary>
    /// <remarks>
    /// Currently not implemented and only exists so mappers can put them in.
    /// </remarks>
    [DataField("airlocks"), ViewVariables(VVAccess.ReadWrite)]
    public List<EntityUid> Airlocks = new();
}
