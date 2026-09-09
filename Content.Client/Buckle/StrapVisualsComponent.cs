namespace Content.Client.Buckle;

/// <summary>
/// Defines foreground layers rendered by a client proxy while this strap is occupied.
/// </summary>
[RegisterComponent]
public sealed partial class StrapVisualsComponent : Component
{
    /// <summary>
    /// Visual layers rendered by the foreground proxy, in drawing order.
    /// </summary>
    [DataField(required: true)]
    public List<PrototypeLayerData> Layers = new();

    /// <summary>
    /// Draw depth used by the foreground proxy.
    /// </summary>
    [DataField(required: true)]
    public int DrawDepth;
}
