
using Content.Shared.Chemistry.EntitySystems;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// Marker that machine can separate solution that it works with into layers,
/// so it will pour out reagent by reagent and not all mixed up.
/// </summary>
/// <seealso cref="SharedSolutionContainerMixerSystem"/>
[RegisterComponent]
public sealed partial class SolutionSeparatorComponent : Component
{
}
