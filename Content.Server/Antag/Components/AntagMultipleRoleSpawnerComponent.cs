using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Antag.Components;

/// <summary>
/// Spawns multiple prototypes for each antag prototype selected by the <see cref="AntagSelectionSystem"/>
/// </summary>
[RegisterComponent]
public sealed partial class AntagMultipleRoleSpawnerComponent : Component
{
    /// <summary>
    ///     Antag prototype -> List of entities to spawn for that prototype
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<AntagPrototype>, List<EntProtoId>> AntagRoleToPrototypes;

    /// <summary>
    ///     Should you remove ent prototypes from the list after spawning one.
    /// </summary>
    [DataField]
    public bool PickAndTake;
}
