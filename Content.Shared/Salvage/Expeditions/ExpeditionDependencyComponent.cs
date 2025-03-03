using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.Salvage.Expeditions;

/// <summary>
/// Cancels a non-active salvage mission when an entity with this component is shut down.
/// </summary>
[RegisterComponent]
public sealed partial class SalvageMissionDependencyComponent : Component
{
    /// <summary>
    /// Uid of the map that is deleted upon this component's shutdown, which by extension shuts down the associated salvage mission, as long as the mission is inactive.
    /// </summary>
    public MapId? AssociatedMapId;
    /// <summary>
    /// The salvage mission that will be deleted upon this component's shutdown, as long as the mission is inactive.
    /// </summary>
    public SalvageMission AssociatedMission;
}
