using Content.Shared.FixedPoint;

namespace Content.Server.Nutrition.Components;

/// <summary>
/// Entities with this component occasionally spill some of their drink when drinking.
/// </summary>
[RegisterComponent]
public sealed partial class MessyDrinkerComponent : Component
{
    [DataField]
    public float SpillChance = 0.2f;

    /// <summary>
    /// The amount of solution that is spilled when <see cref="SpillChance"/> procs.
    /// </summary>
    [DataField]
    public FixedPoint2 SpillAmount = 1.0;

    [DataField]
    public LocId? SpillMessagePopup;
}
