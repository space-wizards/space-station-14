using Content.Server.Antag;
using Content.Shared.Roles;
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

    /// <summary>
    /// List of different entity prototypes for each antag role that is selected by the <see cref="AntagSelectionSystem"/>.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<AntagPrototype>, List<EntProtoId>>? Prototypes;
}
