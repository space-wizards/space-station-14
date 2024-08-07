using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.FoodSequence;

/// <summary>
/// A starting point for the creation of procedural food.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), Access(typeof(SharedFoodSequenceSystem))]
public sealed partial class FoodSequenceStartPointComponent : Component
{
    /// <summary>
    /// A key that determines which types of food elements can be attached to a food.
    /// </summary>
    [DataField]
    public string Key = string.Empty;

    /// <summary>
    /// path to RSI, where sprites of all layers for this type of food are stored
    /// </summary>
    [DataField]
    public string RsiPath = string.Empty;

    /// <summary>
    /// The maximum number of layers of food that can be placed on this item.
    /// </summary>
    [DataField]
    public int MaxLayers = 5;

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
    /// list of sprite states to be displayed on this object.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<string> FoodLayers = new();

    public HashSet<string> RevealedLayers = new();
}

[Serializable, NetSerializable]
public enum FoodSequenceState : byte
{
    State
}
