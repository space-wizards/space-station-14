
namespace Content.Server._Impstation.CosmicCult.Components;

/// <summary>
/// Objective condition that requires the player to be a rogue ascended and corrupt other players' minds.
/// Requires <see cref="NumberObjectiveComponent"/> to function.
/// </summary>
[RegisterComponent, Access(typeof(RogueAscendedObjectiveSystem))]
public sealed partial class RogueInfectionConditionComponent : Component
{
    [DataField]
    public int MindsCorrupted;
}
