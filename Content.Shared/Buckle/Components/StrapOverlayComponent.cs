using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using DrawDepthTag = Robust.Shared.GameObjects.DrawDepth;

namespace Content.Shared.Buckle.Components;

/// <summary>
/// Defines extra visual layers shown while this strap is occupied.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StrapOverlayComponent : Component
{
    /// <summary>
    /// Entity prototype used to render the overlay.
    /// </summary>
    [DataField]
    public EntProtoId OverlayPrototype = "StrapOverlayVisual";

    /// <summary>
    /// Layers shown while the strap is occupied.
    /// </summary>
    [DataField(required: true)]
    public List<PrototypeLayerData> Layers = new();

    /// <summary>
    /// Draw depth used for the overlay.
    /// </summary>
    [DataField(customTypeSerializer: typeof(ConstantSerializer<DrawDepthTag>))]
    public int OverlayDrawDepth = (int) DrawDepth.DrawDepth.OverMobs;
}
