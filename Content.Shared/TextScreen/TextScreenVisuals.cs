using Robust.Shared.Serialization;

namespace Content.Shared.TextScreen;

/// <summary>
/// Layers for text screen sprites.
/// </summary>
[Serializable, NetSerializable]
public enum TextScreenVisualLayers : byte
{
    /// <summary>
    /// A frame to draw over the text on screen to obscure the scrolling effect.
    /// Will be reordered to be on top of the text layers.
    /// </summary>
    Frame
}
