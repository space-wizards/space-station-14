using Content.Server.Nutrition.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Nutrition.Components;

[RegisterComponent, Access(typeof(SliceableFoodSystem))]
public sealed partial class SliceableFoodComponent : Component
{
    /// <summary>
    /// Prototype to spawn after slicing.
    /// If null then it can't be sliced.
    /// </summary>
    [DataField]
    public EntProtoId? Slice;

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Items/Culinary/chop.ogg");

    /// <summary>
    /// Number of slices the food starts with.
    /// </summary>
    [DataField("count")]
    public ushort TotalCount = 5;

    /// <summary>
    /// how long it takes for this food to be sliced
    /// </summary>
    [DataField]
    public float SliceTime = 1f;

    /// <summary>
    /// all the pieces will be shifted in random directions.
    /// </summary>
    [DataField]
    public float SpawnOffset = 2f;

    /// <summary>
    /// For botany produe, should the number of slices be dependant on its potency (true), or static (false). If there is no potency found, defaults to false outcome.
    /// </summary>
    /// <remarks>
    /// for most plants this won't be relevent, as potency will only effect reagent amount which is already accounted for as long as reagents are transferred.
    /// would instead be relevent for stackable produce stuch as towercap or cotton
    /// </remarks>
    [DataField]
    public bool PotencyEffectsCount = false;

    /// <summary>
    /// whether or not any sharp object can be used to cut this (true), or only a knife utensil (false)
    /// </summary>
    [DataField]
    public bool AnySharp = false;
}
