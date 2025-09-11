namespace Content.Client.Clothing;

/// <summary>
/// Communicates folded layers data (currently only Scale to handle flipping)
/// to the wearer clothing sprite layer
/// </summary>
[RegisterComponent]
[Access(typeof(FlippableClothingVisualizerSystem))]
public sealed partial class FlippableClothingVisualsComponent : Component
{
    [DataField]
    public string FoldingLayer = "foldedLayer";

    [DataField]
    public string UnfoldingLayer = "unfoldedLayer";
}
