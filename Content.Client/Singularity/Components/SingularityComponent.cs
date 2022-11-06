using Content.Shared.Singularity.Components;
using Content.Client.Singularity.EntitySystems;

namespace Content.Client.Singularity.Components;

/// <summary>
/// The client-side version of <see cref="SharedSingularityComponent"/>.
/// Primarily managed by <see cref="SingularitySystem"/>.
/// </summary>
[RegisterComponent]
[ComponentReference(typeof(SharedSingularityComponent))]
public sealed class SingularityComponent : SharedSingularityComponent
{}
