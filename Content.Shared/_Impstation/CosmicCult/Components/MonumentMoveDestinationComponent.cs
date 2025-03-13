namespace Content.Shared._Impstation.CosmicCult.Components;

/// <summary>
/// This is used to mark an entity as the end point for the "relocate monument" ability. ideally there should only ever be one of these
/// </summary>
[RegisterComponent]
public sealed partial class MonumentMoveDestinationComponent : Component
{
    public EntityUid? Monument;
}
