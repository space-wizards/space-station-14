namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Marks the glove entity as finger guns. Manages the hidden gun container.
/// </summary>
[RegisterComponent]
public sealed partial class FingerGunsComponent : Component
{
    [DataField]
    public bool SkipGunSpawn;
}

/// <summary>
/// Marks the hidden gun entity as the finger guns weapon.
/// Stores a reference back to the glove version's entity for reverting. I think... I hope.
/// </summary>
[RegisterComponent]
public sealed partial class FingerGunsGunComponent : Component
{
    [DataField]
    public string? OriginalHand;
}
