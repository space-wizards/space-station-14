using System.Numerics;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

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
    public ProtoId<TagPrototype> Key = string.Empty;

    /// <summary>
    /// The maximum number of layers of food that can be placed on this item.
    /// </summary>
    [DataField]
    public int MaxLayers = 10;

    /// <summary>
    /// Can we put more layers?
    /// </summary>
    [DataField]
    public bool Finished;

    /// <summary>
    /// solution where reagents will be added from newly added ingredients
    /// </summary>
    [DataField]
    public string Solution = "food";

    #region name generation

    /// <summary>
    /// LocId with a name generation pattern.
    /// </summary>
    [DataField]
    public LocId? NameGeneration;

    /// <summary>
    /// the part of the name generation used in the pattern
    /// </summary>
    [DataField]
    public LocId? NamePrefix;

    /// <summary>
    /// content in the form of all added ingredients will be separated by these symbols
    /// </summary>
    [DataField]
    public string? ContentSeparator;

    /// <summary>
    /// the part of the name generation used in the pattern
    /// </summary>
    [DataField]
    public LocId? NameSuffix;

    #endregion

    #region visual

    /// <summary>
    /// list of sprite states to be displayed on this object.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<FoodSequenceVisualLayer> FoodLayers = new();

    /// <summary>
    /// If true, the generative layers will be placed in reverse order.
    /// </summary>
    [DataField]
    public bool InverseLayers;

    /// <summary>
    /// target layer, where new layers will be added. This allows you to control the order of generative layers and static layers.
    /// </summary>
    [DataField]
    public string TargetLayerMap = "foodSequenceLayers";

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
    public bool AllowHorizontalFlip = true;

    public HashSet<string> RevealedLayers = new();

    #endregion
}

/// <summary>
/// class that synchronizes with the client
/// Stores all the necessary information for rendering the FoodSequence element
/// </summary>
[DataRecord, Serializable, NetSerializable]
public partial record struct FoodSequenceVisualLayer
{
    /// <summary>
    /// reference to the original prototype of the layer. Used to edit visual layers.
    /// </summary>
    public ProtoId<FoodSequenceElementPrototype> Proto;

    /// <summary>
    /// Sprite rendered in sequence
    /// </summary>
    public SpriteSpecifier? Sprite { get; set; } = SpriteSpecifier.Invalid;

    /// <summary>
    /// Relative size of the sprite displayed in FoodSequence
    /// </summary>
    public Vector2 Scale { get; set; } = Vector2.One;

    /// <summary>
    /// The offset of a particular layer. Allows a little position randomization of each layer.
    /// </summary>
    public Vector2 LocalOffset { get; set; } = Vector2.Zero;

    public FoodSequenceVisualLayer(ProtoId<FoodSequenceElementPrototype> proto,
        SpriteSpecifier? sprite,
        Vector2 scale,
        Vector2 offset)
    {
        Proto = proto;
        Sprite = sprite;
        Scale = scale;
        LocalOffset = offset;
    }
}
