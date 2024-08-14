using System.Numerics;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.GameStates;

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

    /// <summary>
    /// target layer, where new layers will be added. This allows you to control the order of generative layers and static layers.
    /// </summary>
    [DataField]
    public string TargetLayerMap = "foodSequenceLayers";

    /// <summary>
    /// If true, the generative layers will be placed in reverse order.
    /// </summary>
    [DataField]
    public bool InverseLayers;

    /// <summary>
    /// each layer will get a random offset in the specified range
    /// </summary>
    [DataField]
    public Vector2 MaxLayerOffset = Vector2.Zero;

    /// <summary>
    /// each layer will get a random offset in the specified range
    /// </summary>
    [DataField]
    public Vector2 MinLayerOffset = Vector2.Zero;

    [DataField]
    public string Solution = "food";

    [DataField]
    public LocId? NameGeneration;

    [DataField]
    public LocId? NamePrefix;

    [DataField]
    public string? ContentSeparator;

    [DataField]
    public LocId? NameSuffix;
}
