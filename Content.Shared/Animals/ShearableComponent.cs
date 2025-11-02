using Content.Shared.Chemistry.Components;
using Content.Shared.DoAfter;
using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Animals;

/// <summary>
///     Lets an entity be sheared by a tool to consume a reagent to spawn an amount of an item.
///     For example, sheep can be sheared to consume wool Solution to spawn cotton.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ShearableComponent : Component
{
    /// <summary>
    ///     A pre-existing solution inside the target entity that will be subtracted from upon being sheared.
    /// </summary>
    /// <example>
    ///     targetSolutionName: wool
    /// </example>
    [DataField]
    public string TargetSolutionName = string.Empty;

    /// <summary>
    ///     An ID of an entity that will be spawned upon the creature being sheared.
    /// </summary>
    /// <example>
    ///     shearedProductID: MaterialCotton1
    /// </example>
    [DataField]
    public EntProtoId ShearedProductID;

    /// <summary>
    ///     How many products will be spawned per solution.
    ///     A value of 5 will spawn 5 products for every 1u of solution. 25u of solution would spawn 125 product.
    ///     A value of 0.2 will spawn 0.2 products for every 1u of solution. 25u of solution would spawn 5 product.
    ///     Keep in mind, only up to the maximum stack of the specified product will be spawned,
    ///     the remaining solution will be truncated and left unchanged. In these cases the player can shear more than once to get more.
    /// </summary>
    /// <example>
    ///     productsPerSolution: 0.2
    /// </example>
    [DataField]
    public float ProductsPerSolution = 0;

    /// <summary>
    ///     The maximum number of products that can be spawned at once.
    ///     e.g. if the animal has enough solution to spawn 50 items
    ///     but MaximumProductsSpawned is set to 25, then you will need to shear it twice.
    ///     Default/null is infinite.
    /// </summary>
    /// <example>
    ///     maximumProductsSpawned: 25
    /// </example>
    [DataField]
    public int? MaximumProductsSpawned;

    /// <summary>
    ///     Items dropped by shearing an entity can have their placement randomised so they don't all fall on the exact same pixel.
    ///     For each spawn, a random number is chosen between the negative and positive values of this setting.
    ///     If you set it to 0.2 then a random offset between -0.2 and 0.2 is picked for each spawn.
    ///     Setting a value higher than 1 has a chance to spawn items outside the tile the entity stands on, which may go through walls.
    /// </summary>
    /// <example>
    ///     RandomSpawnOffsetVariation: 0.2f
    /// </example>
    [DataField]
    public float RandomSpawnOffsetVariation = 0.2f;

    /// <summary>
    ///     The "Quality" of the target item that allows this entity to be sheared.
    ///     For example, Wirecutters have the "cutting" quality.
    ///     Leave undefined for no tool required.
    /// </summary>
    /// <example>
    ///     toolQuality: Cutting
    /// </example>
    [DataField]
    public ProtoId<ToolQualityPrototype>? ToolQuality;

    /// <summary>
    ///     A LocID that is added to the description box when the entity is shearable.
    /// </summary>
    /// <example>
    ///     shearableMarkupText: sheep-shearable-examine-markup
    /// </example>
    [DataField]
    public LocId? ShearableMarkupText;

    /// <summary>
    ///     A LocID that is added to the description box when the entity is not shearable.
    /// </summary>
    /// <example>
    ///     unShearableMarkupText: sheep-not-shearable-examine-markup
    /// </example>
    [DataField]
    public LocId? UnShearableMarkupText;

    /// <summary>
    ///     A LocID of the verb used for shearing, this is used in some popups in-game. For example "you can't SHEAR that sheep".
    /// </summary>
    /// <example>
    ///     verb: shearable-system-verb-shear
    /// </example>
    [DataField]
    public LocId Verb = "shearable-system-verb-shear";

    /// <summary>
    ///     Specifies the shearing icon that appears in the context menu when right-clicking a shearable animal.
    ///     Defaults to an icon of scissors.
    ///     You can specify a direct path, or an RSI and icon state.
    /// </summary>
    /// <example>
    ///     shearingIcon: /Textures/Interface/VerbIcons/scissors.svg.236dpi.png
    /// </example>
    [DataField]
    public SpriteSpecifier ShearingIcon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/scissors.svg.236dpi.png"));

    /// <summary>
    ///     This is used for handling the shearable layer.
    ///     A special sprite layer that changes based on the mob's life state.
    ///     But can also be toggled by the shearable solution dropping blow a certain amount.
    ///     Typically, when Shearable is True, the shearable layer will be visible.
    /// </summary>
    [DataField]
    public bool Shearable;
}

/// <summary>
///     These are used as return values for CheckShear()
///     Each one represents a different reason for why the target entity cannot be sheared.
///     Except <c>Success</c> which means it can be sheared.
/// </summary>
public enum CheckShearReturns
{
    /// <summary> All checks were successful, the target entity can be sheared. </summary>
    Success,
    /// <summary> The player is not using the correct tool to shear this animal. Or their hand is empty. </summary>
    WrongTool,
    /// <summary> There is not enough solution in the animal to form a single target product. </summary>
    InsufficientSolution,
    /// <summary> Some error ocurred, check your debug log. </summary>
    Error
}

/// <summary>
///     Also part of the Shearable Layer.
/// </summary>
/// <seealso cref="ShearableComponent.Shearable"/>
[Serializable, NetSerializable]
public enum ShearableVisuals
{
    Shearable,
}

/// <summary>
///     Thrown whenever an animal is sheared.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ShearingDoAfterEvent : SimpleDoAfterEvent { }
