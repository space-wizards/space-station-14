using Content.Server.Body.Components;
using Content.Shared.Nutrition.Components;
using Content.Server.Nutrition.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Nutrition.Components;

[RegisterComponent, Access(typeof(FoodSystem), typeof(FoodSequenceSystem))]
public sealed partial class FoodComponent : Component
{
    [DataField]
    public string Solution = "food";

    [DataField]
    public SoundSpecifier UseSound = new SoundCollectionSpecifier("eating");

    [DataField]
    public List<EntProtoId> Trash = new();

    [DataField]
    public FixedPoint2? TransferAmount = FixedPoint2.New(5);

    /// <summary>
    /// Acceptable utensil to use
    /// </summary>
    [DataField]
    public UtensilType Utensil = UtensilType.Fork; //There are more "solid" than "liquid" food

    /// <summary>
    /// Is utensil required to eat this food
    /// </summary>
    [DataField]
    public bool UtensilRequired;

    /// <summary>
    ///     If this is set to true, food can only be eaten if you have a stomach with a
    ///     <see cref="StomachComponent.SpecialDigestible"/> that includes this entity in its whitelist,
    ///     rather than just being digestible by anything that can eat food.
    ///     Whitelist the food component to allow eating of normal food.
    /// </summary>
    [DataField]
    public bool RequiresSpecialDigestion;

    /// <summary>
    ///     Stomachs required to digest this entity.
    ///     Used to simulate 'ruminant' digestive systems (which can digest grass)
    /// </summary>
    [DataField]
    public int RequiredStomachs = 1;

    /// <summary>
    /// The localization identifier for the eat message. Needs a "food" entity argument passed to it.
    /// </summary>
    [DataField]
    public LocId EatMessage = "food-nom";

    /// <summary>
    /// How long it takes to eat the food personally.
    /// </summary>
    [DataField]
    public float Delay = 1;

    /// <summary>
    ///     This is how many seconds it takes to force feed someone this food.
    ///     Should probably be smaller for small items like pills.
    /// </summary>
    [DataField]
    public float ForceFeedDelay = 3;

    /// <summary>
    /// For mobs that are food, requires killing them before eating.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool RequireDead = true;
}
