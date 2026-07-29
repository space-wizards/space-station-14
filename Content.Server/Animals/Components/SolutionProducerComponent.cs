using Content.Server.Animals.Systems;
using Content.Shared.Chemistry.Components;

namespace Content.Server.Animals.Components;

/// <summary>
/// Adds a configured solution when production is requested.
/// This component is intended to be placed directly on a solution entity.
/// </summary>
[RegisterComponent, Access(typeof(SolutionProducerSystem))]
public sealed partial class SolutionProducerComponent : Component
{
    [DataField(required: true)]
    public Solution Generated = default!;
}
