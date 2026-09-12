using Content.Shared.FixedPoint;

namespace Content.Shared.Fluids.Components;

/// <summary>
/// Makes a solution contained in this entity spillable.
/// Spills can occur when a container with this component overflows,
/// is used to melee attack something, is equipped (see <see cref="SpillWorn"/>),
/// lands after being thrown, or has the Spill verb used.
/// </summary>
[RegisterComponent]
public sealed partial class SpillableComponent : Component
{
    [DataField("solution")]
    public string SolutionName = "puddle";

    [DataField]
    public float? SpillDelay;

    /// <summary>
    /// A popup to show instead of the default one on the successful splash.
    /// </summary>
    [DataField]
    public LocId? OnSplashPopup;

    /// <summary>
    /// Whether to play the splash sound.
    /// </summary>
    [DataField]
    public bool PlaySound = true;

    /// <summary>
    /// If you can spill the reagent from the container from your hands with the the spill verb
    /// </summary>
    [DataField]
    public bool CanSpillFromHand = true;

    /// <summary>
    ///     At most how much reagent can be splashed on someone at once?
    /// </summary>
    [DataField]
    public FixedPoint2 MaxMeleeSpillAmount = FixedPoint2.New(20);

    /// <summary>
    ///     Should this item be spilled when thrown?
    /// </summary>
    [DataField]
    public bool SpillWhenThrown = true;

    /// <summary>
    ///     If true, melee processing will stop if any reagent is transferred.
    ///     Otherwise, melee processing keeps occuring allowing both reagent
    ///     transfer and melee damage to happen.
    /// </summary>
    [DataField]
    public bool PreventMelee = true;
}
