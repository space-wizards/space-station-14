using Content.Shared.Hands.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Item;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
[NetworkedComponent]
public sealed partial class ItemVisualizerComponent : Component
{

    /// <summary>
    ///
    /// </summary>
    [DataField]
    public Dictionary<HandLocation, List<PrototypeLayerData>> InhandVisuals = new();

    [DataField]
    public Dictionary<HandLocation, List<PrototypeLayerData>> WieldedInhandVisuals = new();

    [DataField]
    public Dictionary<string, List<PrototypeLayerData>> ClothingVisuals = new();

    /// <summary>
    ///     This is a nested dictionary that maps appearance data keys -> sprite layer keys -> appearance data values -> layer data.
    ///     While somewhat convoluted, this enables the sprite layer data to be completely modified using only yaml.
    ///
    ///     In most instances, each of these dictionaries will probably only have a single entry.
    /// </summary>
    [DataField("visuals", required:true)]
    public Dictionary<Enum, Dictionary<string, Dictionary<string, PrototypeLayerData>>> Visuals = default!;
}
