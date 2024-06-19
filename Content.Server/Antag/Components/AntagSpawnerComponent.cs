using Content.Server.Antag;
using Robust.Shared.Prototypes;

namespace Content.Server.Antag.Components;

/// <summary>
/// Spawns a prototype for antags created with a spawner.
/// </summary>
[RegisterComponent, Access(typeof(AntagSpawnerSystem))]
public sealed partial class AntagSpawnerComponent : Component
{
    /// <summary>
    /// The entity to spawn.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Prototype = string.Empty;
}
