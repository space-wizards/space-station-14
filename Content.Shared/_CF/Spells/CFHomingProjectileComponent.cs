namespace Content.Shared.CF.Spells;

/// <summary>
/// Server-steered bolt. Each tick it flies toward the creature that was clicked,
/// instead of keeping the direction from the moment of the cast.
/// </summary>
[RegisterComponent]
public sealed partial class CFHomingProjectileComponent : Component
{
    [DataField]
    public EntityUid Target;

    [DataField]
    public EntityUid Caster;

    /// <summary>The spell action, so the hit can read <see cref="CFSpellComponent"/>.</summary>
    [DataField]
    public EntityUid Action;

    [DataField]
    public bool Resolved;

    [DataField]
    public float Speed = 18f;
}
