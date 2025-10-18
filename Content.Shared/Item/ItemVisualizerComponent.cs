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
    [DataField]
    public Dictionary<HandLocation, List<PrototypeLayerData>> InhandVisuals = new();

    [DataField]
    public Dictionary<HandLocation, List<PrototypeLayerData>> WieldedInhandVisuals = new();

    [DataField]
    public Dictionary<string, List<PrototypeLayerData>> ClothingVisuals = new();
}
