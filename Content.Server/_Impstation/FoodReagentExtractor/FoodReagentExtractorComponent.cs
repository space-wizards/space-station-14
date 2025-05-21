using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.FoodReagentExtractor;

/// <summary>
///     Extracts specific reagents from food used on component owner, then deletes the food.
/// </summary>
[RegisterComponent, Access(typeof(FoodReagentExtractorSystem))]
public sealed partial class FoodReagentExtractorComponent : Component
{
    /// <summary>
    ///     Solution to move the reagents to.
    /// </summary>
    [DataField(required: true)]
    public string SolutionId = default!;

    /// <summary>
    ///     List of reagents to extract.
    /// </summary>
    [DataField]
    public List<ProtoId<ReagentPrototype>> ExtractionReagents = new()
    {
        "Nutriment"
    };

    /// <summary>
    ///     Sound to play when food is used on this.
    /// </summary>
    [DataField]
    public SoundSpecifier? ExtractSound = new SoundPathSpecifier("/Audio/Effects/waterswirl.ogg");

    /// <summary>
    ///     Popup for the food disappearing.
    /// </summary>
    [DataField]
    public LocId MessageFoodEaten = "food-reagent-extractor-component-eat";

    /// <summary>
    ///     Popup when the target solution was already full.
    /// </summary>
    [DataField]
    public LocId MessageSolutionFull = "food-reagent-extractor-component-full";

    /// <summary>
    ///     Popup for the food that doesn't have what we want.
    /// </summary>
    [DataField]
    public LocId MessageBadFood = "food-reagent-extractor-component-bad-food";
}
