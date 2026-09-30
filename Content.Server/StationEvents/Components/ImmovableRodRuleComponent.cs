using Content.Server.StationEvents.Events;
using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(ImmovableRodRule))]
public sealed partial class ImmovableRodRuleComponent : Component
{
    /// <summary>
    ///     List of possible rods and spawn probabilities.
    /// </summary>
    [DataField]
    public EntityTableSelector RodPrototypes = default!;
}
