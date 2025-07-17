using Content.Shared.Body.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Nutrition.Components;

/// <summary>
/// This is used on an entity with a solution container to flag a specific solution as being able to have its
/// reagents consumed directly.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(IngestionSystem))]
public sealed partial class EdibleComponent : Component
{
    /// <summary>
    /// Name of the solution that stores the consumable reagents
    /// </summary>
    [DataField]
    public string Solution = "food";

    /// <summary>
    /// Should this entity be deleted when our solution is emptied?
    /// </summary>
    [DataField]
    public bool DestroyOnEmpty = true;

    /// <summary>
    /// Trash we spawn when eaten, will not spawn if the item isn't deleted when empty.
    /// </summary>
    [DataField]
    public List<EntProtoId> Trash = new();

    /// <summary>
    /// How much of our solution is eaten on a do-after completion. Set to null to eat the whole thing.
    /// </summary>
    [DataField]
    public FixedPoint2? TransferAmount = FixedPoint2.New(5);

    /// <summary>
    /// Acceptable utensils to use
    /// </summary>
    [DataField]
    public UtensilType Utensil = UtensilType.Fork; //There are more "solid" than "liquid" food

    /// <summary>
    /// Do we need a utensil to access this solution?
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
    /// How long it takes to eat the food personally.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1f);

    /// <summary>
    ///     This is how many seconds it takes to force-feed someone this food.
    ///     Should probably be smaller for small items like pills.
    /// </summary>
    [DataField]
    public TimeSpan ForceFeedDelay = TimeSpan.FromSeconds(3f);

    /// <summary>
    /// For mobs that are food, requires killing them before eating.
    /// </summary>
    [DataField]
    public bool RequireDead = true;

    /// <summary>
    /// Verb, icon, and sound data for our edible.
    /// </summary>
    [DataField]
    public ProtoId<EdiblePrototype> Edible = IngestionSystem.Food;
}
