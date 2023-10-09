namespace Content.Server.Fragile;

/// <summary>
/// Entities with a fragile component are destroyed when they are flushed down a disposals chute
/// </summary>
[RegisterComponent]
public sealed partial class FragileComponent : Component
{
    /// <summary>
    ///     Sound played when the entity is destroyed.
    /// </summary>
    [DataField("splatSound")]
    public string? splatSound;
}
