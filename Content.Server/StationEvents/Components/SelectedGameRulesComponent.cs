using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Server.StationEvents.Components;

/// <summary>
/// All the events that are allowed to run in the current round. If this is not assigned to the game rule it will select from all of them.
/// </summary>
[RegisterComponent]
public sealed partial class SelectedGameRulesComponent : Component
{
    /// <summary>
    /// All the events that are allowed to run in the current round.
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector ScheduledGameRules = default!;
}
