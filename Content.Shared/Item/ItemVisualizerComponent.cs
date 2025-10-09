using Content.Shared.Hands.Components;
using Content.Shared.Item;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Item;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
[NetworkedComponent]
public sealed partial class ItemVisualizerComponent : Component
{

    [DataField("sprite")]
    public string RsiPath;

    [DataField(required:true)]
    public Dictionary<HandLocation, List<PrototypeLayerData>> InhandVisuals = new();

    [DataField(required:true)]
    public Dictionary<HandLocation, List<PrototypeLayerData>> WieldedInhandVisuals = new();

}
