using System.Numerics;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Nutrition.Components;

/// <summary>
/// A starting point for the creation of procedural food.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), Access(typeof(SharedFoodSequenceSystem))]
public sealed partial class FoodSequenceStartPointComponent : Component
{
    /// <summary>
    /// A key that determines which types of food elements can be attached to a food.
    /// </summary>
    [DataField(required: true)]
    public string Key = string.Empty;

    /// <summary>
    /// The maximum number of layers of food that can be placed on this item.
    /// </summary>
    [DataField]
    public int MaxLayers = 10;

    /// <summary>
    /// Start shift from the center of the sprite where the first layer of food will be placed.
    /// </summary>
    [DataField]
    public Vector2 StartPosition = Vector2.Zero;

    /// <summary>
    /// Shift from the start position applied to each subsequent layer.
    /// </summary>
    [DataField]
    public Vector2 Offset = Vector2.Zero;

    /// <summary>
    /// Can we put more layers?
    /// </summary>
    [DataField]
    public bool Finished;

    /// <summary>
    /// list of sprite states to be displayed on this object.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<FoodSequenceElementEntry> FoodLayers = new();

    public HashSet<string> RevealedLayers = new();

    [DataField]
    public string Solution = "food";

    [DataField]
    public LocId? NameGeneration;

    [DataField]
    public LocId? NamePrefix;

    [DataField]
    public LocId? ContentSeparator;

    [DataField]
    public LocId? NameSuffix;
}
