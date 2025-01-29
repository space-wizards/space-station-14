namespace Content.Shared.Projectiles;

/// <summary>
/// Stores a list of all stuck entities to release when this entity is deleted.
/// </summary>
[RegisterComponent]
public sealed partial class EmbeddedContainerComponent : Component
{
    [DataField]
    public HashSet<EntityUid?> EmbeddedObjects = new();
}
