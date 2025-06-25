using Content.Shared.Objectives.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Components;

/// <summary>
/// A condition that requires a player to pet station pets with the [Fill in] component.
/// Requires <see cref="NumberObjectiveComponent"/> to function.
/// </summary>
[RegisterComponent]
public sealed partial class PetStationPetsConditionComponent : Component
{
    /// <summary>
    /// The number of unique pets which have been pet.
    /// </summary>
    [DataField]
    public HashSet<EntityUid> PettedPets = new();
}
