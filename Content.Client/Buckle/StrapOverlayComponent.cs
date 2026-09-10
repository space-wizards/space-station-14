using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using DrawDepthTag = Robust.Shared.GameObjects.DrawDepth;

namespace Content.Client.Buckle;

/// <summary>
/// Defines extra visual layers shown while this strap is occupied.
/// </summary>
[RegisterComponent]
public sealed partial class StrapOverlayComponent : Component
{
    /// <summary>
    /// Layers shown while the strap is occupied.
    /// </summary>
    [DataField(required: true)]
    public List<PrototypeLayerData> Layers = new();

    /// <summary>
    /// Draw depth used for the overlay.
    /// </summary>
    [DataField(required: true, customTypeSerializer: typeof(ConstantSerializer<DrawDepthTag>))]
    public int DrawDepth;

    /// <summary>
    /// Entity used to render the overlay.
    /// </summary>
    public EntityUid? Proxy;
}
