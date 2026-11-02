namespace Content.Shared.CF.Combat;

/// <summary>
/// Marks that flanking added <c>StatusIconComponent</c> and should remove it when the flank ends.
/// Creatures that already had the component (players) keep it.
/// </summary>
[RegisterComponent]
public sealed partial class CFAddedStatusIconComponent : Component;
