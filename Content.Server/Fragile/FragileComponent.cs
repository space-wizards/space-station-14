namespace Content.Server.Fragile;

/// <summary>
/// Entities with a fragile component are destroyed when they are flushed down a disposals chute
/// </summary>
[RegisterComponent]
public sealed partial class FragileComponent : Component
{
    /// <summary>
    ///     Used to create a puddle when this entity is flushed
    /// </summary>
    [DataField("puddle")]
    public string? puddle;
}
