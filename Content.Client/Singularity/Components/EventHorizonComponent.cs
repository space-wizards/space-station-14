using Content.Shared.Singularity.Components;
using Content.Client.Singularity.EntitySystems;

namespace Content.Client.Singularity.Components;

/// <summary>
/// The client-side version of <see cref="SharedEventHorizonComponent"/>.
/// Primarily managed by <see cref="EventHorizonSystem"/>.
/// Exists to let the client know about event horizons and their effects on collision.
/// </summary>
[RegisterComponent]
[ComponentReference(typeof(SharedEventHorizonComponent))]
public sealed class EventHorizonComponent : SharedEventHorizonComponent
{}
