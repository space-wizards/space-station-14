using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Antag.Components;

/// <summary>
/// Selects and spawns one prototype from a list for each antag prototype selected by the <see cref="AntagSelectionSystem"/>
/// </summary>
[RegisterComponent]
public sealed partial class AntagMultipleRoleSpawnerComponent : Component
{
    /// <summary>
    ///     antag prototype -> list of possible entities to spawn for that antag prototype. Will choose from the list randomly once with replacement unless <see cref="PickAndTake"/> is set to true
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<AntagPrototype>, List<EntProtoId>> AntagRoleToPrototypes;

    /// <summary>
    ///     Should you remove ent prototypes from the list after spawning one.
    /// </summary>
    [DataField]
    public bool PickAndTake;
}
