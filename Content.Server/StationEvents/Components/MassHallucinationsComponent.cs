using Content.Server.StationEvents.Events;

namespace Content.Server.StationEvents.Components;

/// <summary>
/// This is used to keep track of hallucinated entities to remove effects when event ends
/// </summary>
[RegisterComponent, Access(typeof(MassHallucinationsRule))]
public sealed partial class MassHallucinationsComponent : Component
{
}
