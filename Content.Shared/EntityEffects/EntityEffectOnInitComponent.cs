namespace Content.Shared.EntityEffects;

/// <summary>
/// Applies a set of EntityEffects to the entity upon map initialization.
/// <remarks>Useful for e.g. station spawning.</remarks>
/// </summary>
[RegisterComponent]
public sealed partial class EntityEffectOnInitComponent : Component
{
    /// <summary>
    /// Effects that should be applied upon the map being initiated.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] Effects { get; set; } = default!;
}
