using Robust.Shared.Containers;

namespace Content.Shared.VentCraw.Tube.Components;

/// <summary>
/// A component representing a vent that you can crawl through
/// </summary>
[RegisterComponent]
public sealed partial class VentCrawTubeComponent : Component
{
    [DataField("containerId")] 
    public string ContainerId { get; set; } = "VentCrawTube";

    [DataField("connected")]
    public bool Connected;

    [ViewVariables]
    public Container Contents { get; set; } = null!;
}

[ByRefEvent]
public record struct GetVentCrawsConnectableDirectionsEvent
{
    public Direction[] Connectable;
}
