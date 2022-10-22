using Content.Shared.Singularity.Components;

namespace Content.Client.Singularity.Components;

[RegisterComponent]
[ComponentReference(typeof(SharedEventHorizonComponent))]
public sealed class EventHorizonComponent : SharedEventHorizonComponent
{}
